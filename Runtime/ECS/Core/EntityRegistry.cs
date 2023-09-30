using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Puffercat.Uxt.Algorithms;
using UnityEngine;
using UnityEngine.Pool;

namespace Puffercat.Uxt.ECS.Core
{
    public delegate void ComponentDestructionCallback(Entity entity);

    public sealed class EntityRegistry
    {
        private readonly FreeListIntSparseMap<short> m_entityArchetypeIds = new();
        private readonly IntSparseMap<ulong> m_entityVersions = new();

        private readonly ComponentRegistry[] m_componentRegistries =
            new ComponentRegistry[ComponentTypeIdRegistry.MaxNumTypes];

        private readonly List<ComponentEvent> m_componentEvents = new();
        private readonly List<Entity> m_entitiesToDestroy = new();

        private readonly ComponentDestructionBuffer m_componentDestructionBuffer = new();
        private readonly ComponentDestructionCallbackTable m_componentDestructionCallbackTable = new();

        public Entity CreateEntity()
        {
            // An entity starts with no components, so it always belong to the empty archetype.
            var entityId = m_entityArchetypeIds.Add(Archetype.EmptyArchetypeId);
            var version = 0ul;
            if (!m_entityVersions.ContainsKey(entityId))
            {
                m_entityVersions.Add(entityId, 1);
                version = 1;
            }
            else
            {
                version = unchecked(++m_entityVersions.AtUnchecked(entityId));
            }

            return new Entity(entityId, version);
        }

        public bool IsNull(Entity entity)
        {
            if (entity.version == 0)
            {
                return true;
            }

            var currentVersion = m_entityVersions.TryGetValue(entity.id, out var found);

            return !found || currentVersion != entity.version;
        }

        public ComponentDestructionCallbackHandle AddComponentDestructionCallback<T>(
            Entity entity,
            ComponentDestructionCallback callback) where T : struct, IEntityComponent<T>
        {
            if (IsNull(entity) || !HasComponent(entity, ComponentTypeId<T>.Value))
            {
                return default;
            }
            return m_componentDestructionCallbackTable.AddCallbackUnchecked(entity, ComponentTypeId<T>.Value, callback);
        }

        /// <summary>
        /// Tries to get a reference to a component of an entity. This function does not check
        /// whether the entity is null or not.
        /// </summary>
        /// <param name="entity">The entity to get component from</param>
        /// <typeparam name="T">The type of component to get</typeparam>
        /// <returns>A reference to the component (or a dummy if not found)</returns>
        internal OptionalRef<T> TryGetComponentUnchecked<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            var compRegistry = GetOrCreateComponentRegistry<T>();
            return compRegistry.TryGetComponent<T>(entity);
        }

        public ref T AddOrGetComponent<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            if (IsNull(entity))
            {
                throw new Exception("Invalid entity");
            }

            return ref AddOrGetComponentUnchecked<T>(entity);
        }

        public OptionalRef<T> TryGetComponent<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            var compRegistry = GetOrCreateComponentRegistry<T>();
            return compRegistry.TryGetComponent<T>(entity);
        }

        public bool HasComponent(Entity entity, ComponentTypeId typeId)
        {
            if (IsNull(entity))
            {
                return false;
            }

            return TryGetComponentRegistry(typeId, out var compRegistry) && compRegistry.HasComponent(entity);
        }

        public bool MarkComponentForRemoval<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            return MarkComponentForRemoval(entity, ComponentTypeId<T>.Value);
        }

        public bool MarkComponentForRemoval(Entity entity, ComponentTypeId typeId)
        {
            if (IsNull(entity))
            {
                throw new Exception("Entity is invalid");
            }

            if (!TryGetComponentRegistry(typeId, out var compRegistry))
            {
                return false;
            }

            if (!compRegistry.HasComponent(entity))
            {
                return false;
            }

            m_componentDestructionBuffer.QueueDestructionUnchecked(entity, typeId);
            return true;
        }

        /// <summary>
        /// Adds to, or get from an entity a component of type <typeparamref name="T"/>.
        /// This function does not check if the entity is alive or not.
        /// </summary>
        /// <param name="entity">The entity to add component to</param>
        /// <typeparam name="T">The type of component to add</typeparam>
        /// <returns></returns>
        internal ref T AddOrGetComponentUnchecked<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            var compRegistry = GetOrCreateComponentRegistry<T>();
            ref var comp = ref compRegistry.AddOrGetComponent<T>(entity, out var isNewComponent);

            if (isNewComponent)
            {
                ref var archetypeId = ref m_entityArchetypeIds.At(entity.id);
                var newArchetypeId = Archetype.Transition_AddComponent(
                    archetypeId, ComponentTypeId<T>.Value);
                archetypeId = newArchetypeId;

                QueueComponentEvent<T>(entity, ComponentEventType.CreationOrModification);
            }

            return ref new OptionalRef<T>(ref comp).Value;
        }

        public int CountComponent<T>() where T : struct, IEntityComponent<T>
        {
            return CountComponent(ComponentTypeId<T>.Value);
        }

        public int CountComponent(ComponentTypeId typeId)
        {
            if (TryGetComponentRegistry(typeId, out var compRegistry))
            {
                return compRegistry.Count;
            }

            return 0;
        }

        public IEnumerable<Entity> GetAllEntitiesWithComponent<T>() where T : struct, IEntityComponent<T>
        {
            return GetAllEntitiesWithComponent(ComponentTypeId<T>.Value);
        }

        public IEnumerable<Entity> GetAllEntitiesWithComponent(ComponentTypeId componentTypeId)
        {
            if (TryGetComponentRegistry(componentTypeId, out var componentRegistry))
            {
                return componentRegistry.Entities;
            }

            return Enumerable.Empty<Entity>();
        }

        public void MarkEntityForDestruction(Entity entity)
        {
            if (IsNull(entity))
            {
                return;
            }

            m_entitiesToDestroy.Add(entity);
        }

        public void ProcessDestruction()
        {
            /*
             * Algorithm:
             *  1.  Generate all the component destruction commands in order to strip all the entities-to-be
             *      destroyed of their components.
             *  2.  Bin those commands by component type.
             *  3.  Invoke the `onComponentDestroy` callbacks for each of the entity-component pair. This involves
             *      a lot of hashmap lookup so you should use `OnComponentDestroy<TComponent>(entity, callback)` with
             *      caution. Use observers instead if possible.
             *  4.  Send the binned commands to each component registries for destruction (potentially in
             *      parallel).
             *  5.  Invoke callbacks on entities that have been destroyed
             *  6.  Destroy them
             *  7.  Update the archetypes of the surviving entities
             */

            using var _0 = ListPool<EntityDestructionCommand>.Get(out var entityDestructionCommands);
            MarkComponentsOfDyingEntitiesForDestruction(entityDestructionCommands);
            m_componentDestructionBuffer.SortAndRemoveDuplicates();
            m_componentDestructionBuffer.InvokeCallbacksIn(m_componentDestructionCallbackTable);

            foreach (var (typeId, entityList) in m_componentDestructionBuffer)
            {
                if (TryGetComponentRegistry(typeId, out var componentRegistry))
                {
                    foreach (var entity in entityList)
                    {
                        componentRegistry.RemoveComponentUnchecked(entity);
                    }
                }
            }

            foreach (var (typeId, entityList) in m_componentDestructionBuffer)
            {
                foreach (var entity in entityList)
                {
                    ref var archId = ref m_entityArchetypeIds.AtUnchecked(entity.id);
                    archId = Archetype.Transition_RemoveComponent(archId, typeId);
                }
            }

            foreach (var entityDestructionCommand in entityDestructionCommands)
            {
                // TODO: call entity destruction callback
            }

            foreach (var entityDestructionCommand in entityDestructionCommands)
            {
                m_entityArchetypeIds.Remove(entityDestructionCommand.entity.id);
                m_entityVersions.AtUnchecked(entityDestructionCommand.entity.id)++;
            }
            
            m_componentDestructionBuffer.Clear();
        }

        private void MarkComponentsOfDyingEntitiesForDestruction(List<EntityDestructionCommand> outDestructionCommands)
        {
            outDestructionCommands.Clear();
            outDestructionCommands.AddRange(
                m_entitiesToDestroy
                    .Select(e => new EntityDestructionCommand(e, m_entityArchetypeIds.At(e.id))));

            m_entitiesToDestroy.Clear();

            // Sort the destruction controls, then remove duplicates
            outDestructionCommands.Sort();
            outDestructionCommands.RemoveRange(outDestructionCommands.DistinctRange(0, outDestructionCommands.Count));


            foreach (var destructionCommand in outDestructionCommands)
            {
                var archetype = Archetype.GetById(destructionCommand.archetypeId);
                foreach (var typeId in archetype.ComponentTypes)
                {
                    // Unchecked destruction is OK since we know the archetype of the entity (i.e. it certainly has
                    // the said component)
                    m_componentDestructionBuffer.QueueDestructionUnchecked(destructionCommand.entity, typeId);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComponentRegistry GetOrCreateComponentRegistry<T>() where T : struct, IEntityComponent<T>
        {
            var typeId = ComponentTypeId<T>.Value;
            return m_componentRegistries[typeId] ??
                   (m_componentRegistries[typeId] = ComponentRegistry.Create<T>());
        }

        private bool TryGetComponentRegistry(ComponentTypeId typeId, out ComponentRegistry registry)
        {
            registry = m_componentRegistries[typeId];
            return registry != null;
        }

        private void QueueComponentEvent<T>(Entity entity, ComponentEventType eventType)
            where T : struct, IEntityComponent<T>
        {
            QueueComponentEvent(entity, ComponentTypeId<T>.Value, eventType);
        }

        private void QueueComponentEvent(Entity entity, ComponentTypeId typeId, ComponentEventType eventType)
        {
            m_componentEvents.Add(new ComponentEvent(entity, typeId, eventType, m_componentEvents.Count));
        }

        private readonly struct EntityDestructionCommand : IComparable<EntityDestructionCommand>, IComparable
        {
            public int CompareTo(EntityDestructionCommand other)
            {
                var archetypeIdComparison = archetypeId.CompareTo(other.archetypeId);
                if (archetypeIdComparison != 0) return archetypeIdComparison;
                return entity.CompareTo(other.entity);
            }

            public int CompareTo(object obj)
            {
                if (ReferenceEquals(null, obj)) return 1;
                return obj is EntityDestructionCommand other
                    ? CompareTo(other)
                    : throw new ArgumentException($"Object must be of type {nameof(EntityDestructionCommand)}");
            }

            public static bool operator <(EntityDestructionCommand left, EntityDestructionCommand right)
            {
                return left.CompareTo(right) < 0;
            }

            public static bool operator >(EntityDestructionCommand left, EntityDestructionCommand right)
            {
                return left.CompareTo(right) > 0;
            }

            public static bool operator <=(EntityDestructionCommand left, EntityDestructionCommand right)
            {
                return left.CompareTo(right) <= 0;
            }

            public static bool operator >=(EntityDestructionCommand left, EntityDestructionCommand right)
            {
                return left.CompareTo(right) >= 0;
            }

            public readonly short archetypeId;
            public readonly Entity entity;

            public EntityDestructionCommand(Entity entity, short archetypeId)
            {
                this.entity = entity;
                this.archetypeId = archetypeId;
            }
        }
    }

    public sealed class ComponentRegistry
    {
        private struct ComponentRecord<T> where T : struct, IEntityComponent<T>
        {
        }

        // The type id of the component type stored in this registry
        private readonly ComponentTypeId m_typeId;

        // Stores the permanent link mapping entity IDs to the actual component data
        private readonly IntSparseMap<EntityComponentLink> m_entityToComponentLinks = new();

        // The array storing actual component data
        private CompactArrayListBase m_components;

        // The array storing components' owning entity's ID
        private readonly CompactArrayList<Entity> m_componentOwners = new();

        /// <summary>
        /// Returns the number of components stored in this registry
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_componentOwners.Count;
        }

        public IEnumerable<Entity> Entities => m_componentOwners;

        // All the component manipulation functions below do not check for whether the entity
        // is alive (i.e. its version is current). This should be guaranteed by the caller!

        /// <summary>
        /// Creates a component registry that stores a certain type of component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ComponentRegistry Create<T>() where T : struct, IEntityComponent<T>
        {
            var registry = new ComponentRegistry(ComponentTypeId<T>.Value)
            {
                m_components = new CompactArrayList<T>()
            };
            return registry;
        }

        private ComponentRegistry(ComponentTypeId typeId)
        {
            m_typeId = typeId;
        }

        public OptionalRef<T> TryGetComponent<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            var e2CLink = m_entityToComponentLinks.TryGetValue(entity.id);
            if (!e2CLink.HasValue)
            {
                return default;
            }

            var componentArray = CastComponentArray<T>();
            return new OptionalRef<T>(ref componentArray.At(e2CLink.Value.componentAddress));
        }

        public bool HasComponent(Entity entity)
        {
            return m_entityToComponentLinks.ContainsKey(entity.id);
        }

        public ref T AddComponent<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            if (m_entityToComponentLinks.ContainsKey(entity.id))
            {
                throw new Exception($"Entity {entity.id} already has component {typeof(T)}");
            }

            return ref AddComponentUnsafe<T>(entity);
        }

        public ref T AddOrGetComponent<T>(Entity entity, out bool isNewComponent) where T : struct, IEntityComponent<T>
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            ref var e2CLink = ref m_entityToComponentLinks.TryGetValue(entity.id, out var success);
            if (success)
            {
                var componentArray = CastComponentArray<T>();
                isNewComponent = false;
                return ref componentArray.At(e2CLink.componentAddress);
            }
            else
            {
                isNewComponent = true;
                return ref AddComponentUnsafe<T>(entity);
            }
        }

        private ref T AddComponentUnsafe<T>(Entity entity) where T : struct, IEntityComponent<T>
        {
            var componentArray = CastComponentArray<T>();
            var componentAddress = componentArray.Count;
            m_entityToComponentLinks.Add(entity.id, new EntityComponentLink(componentAddress));
            m_componentOwners.Add(entity);
            return ref componentArray.Add(default);
        }

        public void RemoveComponentUnchecked(Entity entity)
        {
            // TODO: Insert optional instrumentation to check whether entity has the said component

            var e2CLink = m_entityToComponentLinks.AtUnchecked(entity.id);
            var fillerEntity = m_componentOwners.AtUnchecked(m_componentOwners.Count - 1);
            m_entityToComponentLinks.AtUnchecked(fillerEntity.id).componentAddress = e2CLink.componentAddress;
            m_componentOwners.RemoveAt(e2CLink.componentAddress);
            m_components.RemoveAt(e2CLink.componentAddress);
            m_entityToComponentLinks.Remove(entity.id);
        }

        private CompactArrayList<T> CastComponentArray<T>() where T : struct, IEntityComponent<T>
        {
            return (CompactArrayList<T>)m_components;
        }
    }
}