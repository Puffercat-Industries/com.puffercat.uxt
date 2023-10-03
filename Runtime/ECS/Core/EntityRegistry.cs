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
        private readonly Archetype.Database m_archetypeDatabase = new();

        private readonly FreeListIntSparseMap<short> m_entityArchetypeIds = new();
        private readonly IntSparseMap<ulong> m_entityVersions = new();

        private readonly List<ComponentRegistryBase> m_componentRegistries = new();
        private readonly List<ComponentTypeErasedFunctions> m_componentTypeErasedFunctionTable = new();

        private readonly List<ComponentEvent> m_componentEvents = new();
        private readonly List<Entity> m_entitiesToDestroy = new();

        private readonly ComponentDestructionBuffer m_componentDestructionBuffer = new();
        private readonly ComponentDestructionCallbackTable m_componentDestructionCallbackTable = new();

        private delegate void CopyComponentFunc(Entity dst, Entity src);

        /// <summary>
        /// Stores a list of functions that preserves necessary type information to perform
        /// operations when type information is unavailable.
        /// </summary>
        private class ComponentTypeErasedFunctions
        {
            public CopyComponentFunc copyComponentUncheckedFunc;
        }

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

        /// <summary>
        /// Create a new entity that is a copy of <paramref name="src"/> (i.e. all
        /// of its components are copied).
        /// </summary>
        /// <param name="src">The entity to copy</param>
        public Entity CopyEntity(Entity src)
        {
            if (IsNull(src))
            {
                return default;
            }

            var dst = CreateEntity();
            var archetype = m_archetypeDatabase.GetById(m_entityArchetypeIds.AtUnchecked(src.id));
            foreach (var typeId in archetype.ComponentTypes)
            {
                CopyComponentUnchecked(dst, src, typeId);
            }

            return dst;
        }

        /// <summary>
        /// Copies the component of the given <paramref name="typeId"/> on <paramref name="src"/> entity
        /// to <paramref name="dst"/> entity. This does not check whether src has the component or
        /// whether dst already has the component.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <param name="typeId"></param>
        private void CopyComponentUnchecked(Entity dst, Entity src, ComponentTypeId typeId)
        {
            m_componentTypeErasedFunctionTable[typeId].copyComponentUncheckedFunc?.Invoke(dst, src);
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

        public bool RemoveComponentDestructionCallback(ComponentDestructionCallbackHandle callbackHandle)
        {
            return m_componentDestructionCallbackTable.RemoveCallback(callbackHandle);
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
            return compRegistry.TryGetComponent(entity);
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
            ref var comp = ref compRegistry.AddOrGetComponent(entity, out var isNewComponent);

            if (isNewComponent)
            {
                ref var archetypeId = ref m_entityArchetypeIds.At(entity.id);
                var newArchetypeId = m_archetypeDatabase.Transition_AddComponent(
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
                    componentRegistry.RemoveComponentBulkUnchecked(entityList);
                }
            }

            foreach (var (typeId, entityList) in m_componentDestructionBuffer)
            {
                foreach (var entity in entityList)
                {
                    ref var archId = ref m_entityArchetypeIds.AtUnchecked(entity.id);
                    archId = m_archetypeDatabase.Transition_RemoveComponent(archId, typeId);
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
                var archetype = m_archetypeDatabase.GetById(destructionCommand.archetypeId);
                foreach (var typeId in archetype.ComponentTypes)
                {
                    // Unchecked destruction is OK since we know the archetype of the entity (i.e. it certainly has
                    // the said component)
                    m_componentDestructionBuffer.QueueDestructionUnchecked(destructionCommand.entity, typeId);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComponentRegistry<T> GetOrCreateComponentRegistry<T>() where T : struct, IEntityComponent<T>
        {
            var typeId = ComponentTypeId<T>.Value;
            while (typeId >= m_componentRegistries.Count)
            {
                m_componentRegistries.Add(null);
                m_componentTypeErasedFunctionTable.Add(null);
            }

            if (m_componentRegistries[typeId] is ComponentRegistry<T> compRegistry)
            {
                return compRegistry;
            }

            compRegistry = new ComponentRegistry<T>();
            m_componentTypeErasedFunctionTable[typeId] = CreateTypeErasedFunctions<T>();
            m_componentRegistries[typeId] = compRegistry;
            return compRegistry;
        }

        private ComponentTypeErasedFunctions CreateTypeErasedFunctions<T>() where T : struct, IEntityComponent<T>
        {
            return new ComponentTypeErasedFunctions
            {
                copyComponentUncheckedFunc = (dst, src) =>
                {
                    ref var dstComp = ref AddOrGetComponentUnchecked<T>(dst);
                    ref var srcComp = ref AddOrGetComponentUnchecked<T>(src);
                    dstComp = srcComp.Copy();
                }
            };
        }

        private bool TryGetComponentRegistry(ComponentTypeId typeId, out ComponentRegistryBase registry)
        {
            if (typeId >= m_componentRegistries.Count)
            {
                registry = null;
                return false;
            }

            registry = m_componentRegistries[typeId];
            return registry != null;
        }

        /// <summary>
        /// Gets the component registry for a certain type of component. This function does not check
        /// whether the said component registry has already been created or not (so it can return null). 
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        private ComponentRegistryBase GetComponentRegistryUnchecked(ComponentTypeId typeId)
        {
            Debug.Assert(m_componentRegistries[typeId] != null);

            return m_componentRegistries[typeId];
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

    internal abstract class ComponentRegistryBase
    {
        // Stores the permanent link mapping entity IDs to the actual component data
        protected readonly IntSparseMap<EntityComponentLink> m_entityToComponentLinks = new();

        // The array storing components' owning entity's ID
        protected readonly CompactArrayList<Entity> m_componentOwners = new();

        // The type id of the component type stored in this registry
        protected ComponentTypeId m_typeId = default;

        public IEnumerable<Entity> Entities => m_componentOwners;

        /// <summary>
        /// Returns the number of components stored in this registry
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_componentOwners.Count;
        }

        /// <summary>
        /// Checks if an entity has 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasComponent(Entity entity)
        {
            return m_entityToComponentLinks.ContainsKey(entity.id);
        }

        /// <summary>
        /// Removes the component stored in this registry from a list of <paramref name="entities"/>.
        /// The caller shall ensure that there is no duplication in the list and all entities have the said
        /// component.
        /// </summary>
        /// <param name="entities">The list of entities from which this component shall be removed</param>
        /// <remarks>The bulk operation saves on the cost of multiple virtual function calls</remarks>
        public abstract void RemoveComponentBulkUnchecked(List<Entity> entities);
    }

    internal sealed class ComponentRegistry<T> : ComponentRegistryBase where T : struct, IEntityComponent<T>
    {
        // The array storing actual component data
        private CompactArrayList<T> m_components = new();

        public ComponentRegistry()
        {
            m_typeId = ComponentTypeId<T>.Value;
        }

        // All the component manipulation functions below do not check for whether the entity
        // is alive (i.e. its version is current). This should be guaranteed by the caller!

        public OptionalRef<T> TryGetComponent(Entity entity)
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            var e2CLink = m_entityToComponentLinks.TryGetValue(entity.id);
            if (!e2CLink.HasValue)
            {
                return default;
            }

            return new OptionalRef<T>(ref m_components.At(e2CLink.Value.componentAddress));
        }

        public ref T AddComponent(Entity entity)
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            if (m_entityToComponentLinks.ContainsKey(entity.id))
            {
                throw new Exception($"Entity {entity.id} already has component {typeof(T)}");
            }

            return ref AddComponentUnchecked(entity);
        }

        public ref T AddOrGetComponent(Entity entity, out bool isNewComponent)
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            ref var e2CLink = ref m_entityToComponentLinks.TryGetValue(entity.id, out var success);
            if (success)
            {
                isNewComponent = false;
                return ref m_components.At(e2CLink.componentAddress);
            }
            else
            {
                isNewComponent = true;
                return ref AddComponentUnchecked(entity);
            }
        }

        private ref T AddComponentUnchecked(Entity entity)
        {
            var componentAddress = m_components.Count;
            m_entityToComponentLinks.Add(entity.id, new EntityComponentLink(componentAddress));
            m_componentOwners.Add(entity);
            return ref m_components.Add(default);
        }

        /// <summary>
        /// Removes the component stored in this registry from a list of <paramref name="entities"/>.
        /// The caller shall ensure that there is no duplication in the list and all entities have the said
        /// component.
        /// </summary>
        /// <param name="entities">The list of entities from which this component shall be removed</param>
        /// <remarks>The bulk operation saves on the cost of multiple virtual function calls</remarks>
        public override void RemoveComponentBulkUnchecked(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                RemoveComponentUnchecked(entity);
            }
        }

        private void RemoveComponentUnchecked(Entity entity)
        {
            // TODO: Insert optional instrumentation to check whether entity has the said component

            var e2CLink = m_entityToComponentLinks.AtUnchecked(entity.id);
            var fillerEntity = m_componentOwners.AtUnchecked(m_componentOwners.Count - 1);
            m_entityToComponentLinks.AtUnchecked(fillerEntity.id).componentAddress = e2CLink.componentAddress;
            m_componentOwners.RemoveAt(e2CLink.componentAddress);
            m_components.RemoveAt(e2CLink.componentAddress);
            m_entityToComponentLinks.Remove(entity.id);
        }
    }
}