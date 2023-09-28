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
        private readonly IntSparseMap<ulong> m_entityVersions;

        private readonly ComponentRegistry[] m_componentRegistries =
            new ComponentRegistry[ComponentTypeIdRegistry.MaxNumTypes];

        private readonly List<ComponentEvent> m_componentEvents = new();
        private readonly List<Entity> m_entitiesToDestroy = new();

        private readonly ComponentDestructionBuffer m_componentDestructionBuffer;
        
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
                version = unchecked(++m_entityVersions.At(entityId));
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

        /// <summary>
        /// Tries to get a reference to a component of an entity. This function does not check
        /// whether the entity is null or not.
        /// </summary>
        /// <param name="entity">The entity to get component from</param>
        /// <typeparam name="T">The type of component to get</typeparam>
        /// <returns>A reference to the component (or a dummy if not found)</returns>
        internal OptionalRef<T> TryGetComponentUnsafe<T>(Entity entity) where T : struct, IComponent
        {
            var compRegistry = GetOrCreateComponentRegistry<T>();
            return compRegistry.TryGetComponent<T>(entity);
        }

        internal ref T AddOrComponentUnsafe<T>(Entity entity) where T : struct, IComponent
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

        internal void RemoveComponent(Entity entity, ComponentTypeId componentTypeId)
        {
            if (!TryGetComponentRegistry(componentTypeId, out var compRegistry))
            {
                return;
            }

            ref var archetypeId = ref m_entityArchetypeIds.At(entity.id);
            var newArchetypeId = Archetype.Transition_RemoveComponent(archetypeId, componentTypeId);

            if (newArchetypeId == Archetype.ErrorArchetypeId)
            {
                return;
            }

            archetypeId = newArchetypeId;
            compRegistry.RemoveComponent(entity);
        }

        public void DestroyEntityDeferred(Entity entity)
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
             */
            
            MarkComponentsOfDyingEntitiesForDestruction();
            m_componentDestructionBuffer.SortAndRemoveDuplicates();
        }

        private void MarkComponentsOfDyingEntitiesForDestruction()
        {
            using var _0 = ListPool<EntityDestructionCommand>.Get(out var destructionCommands);

            destructionCommands.AddRange(
                m_entitiesToDestroy
                    .Select(e => new EntityDestructionCommand(e, m_entityArchetypeIds.At(e.id))));

            m_entitiesToDestroy.Clear();

            // Sort the destruction controls, then remove duplicates
            destructionCommands.Sort();
            destructionCommands.RemoveRange(destructionCommands.DistinctRange(0, destructionCommands.Count));


            foreach (var destructionCommand in destructionCommands)
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

        public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
        {
            throw new System.NotImplementedException();
        }

        public ref T AddComponent<T>(Entity entity) where T : struct, IComponent
        {
            throw new System.NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComponentRegistry GetOrCreateComponentRegistry<T>() where T : struct, IComponent
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

        private void QueueComponentEvent<T>(Entity entity, ComponentEventType eventType) where T : struct, IComponent
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
        private struct ComponentRecord<T> where T : struct, IComponent
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

        // All the component manipulation functions below do not check for whether the entity
        // is alive (i.e. its version is current). This should be guaranteed by the caller!

        /// <summary>
        /// Creates a component registry that stores a certain type of component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ComponentRegistry Create<T>() where T : struct, IComponent
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

        public OptionalRef<T> TryGetComponent<T>(Entity entity) where T : struct, IComponent
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

        public ref T AddComponent<T>(Entity entity) where T : struct, IComponent
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            if (m_entityToComponentLinks.ContainsKey(entity.id))
            {
                throw new Exception($"Entity {entity.id} already has component {typeof(T)}");
            }

            return ref AddComponentUnsafe<T>(entity);
        }

        public ref T AddOrGetComponent<T>(Entity entity, out bool isNewComponent) where T : struct, IComponent
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

        private ref T AddComponentUnsafe<T>(Entity entity) where T : struct, IComponent
        {
            var componentArray = CastComponentArray<T>();
            var componentAddress = componentArray.Count;
            m_entityToComponentLinks.Add(entity.id, new EntityComponentLink(componentAddress));
            m_componentOwners.Add(entity);
            return ref componentArray.Add(default);
        }

        public bool RemoveComponent(Entity entity)
        {
            ref var e2CLink = ref m_entityToComponentLinks.TryGetValue(entity.id, out var hasComponent);
            if (!hasComponent)
            {
                return false;
            }

            // TODO: fix up e2clink for the swapped in entity

            // Note: this is virtual function call which can be expensive
            m_components.RemoveAt(e2CLink.componentAddress);

            m_componentOwners.RemoveAt(e2CLink.componentAddress);
            m_entityToComponentLinks.Remove(entity.id);

            return true;
        }

        private CompactArrayList<T> CastComponentArray<T>() where T : struct, IComponent
        {
            return (CompactArrayList<T>)m_components;
        }
    }
}