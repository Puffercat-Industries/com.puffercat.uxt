using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Puffercat.Uxt.ECS.Core
{
    public sealed class EntityRegistry
    {
        private readonly FreeListIntSparseMap<short> m_entityArchetypeIds = new();
        private readonly IntSparseMap<uint> m_entityVersions;

        private readonly ComponentRegistry[] m_componentRegistries =
            new ComponentRegistry[ComponentTypeIdRegistry.MaxNumTypes];

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
        /// Tries to get a reference to a component of an entity. If it does not have the component, return
        /// a reference to a dummy and set <paramref name="success"/> to false. This function does not check
        /// whether the entity is null or not.
        /// </summary>
        /// <param name="entity">The entity to get component from</param>
        /// <param name="success">Whether the entity has this component</param>
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
            return ref new OptionalRef<T>(ref comp).Value;
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
    }

    public sealed class ComponentRegistry
    {
        private struct ComponentRecord<T> where T : struct, IComponent
        {
        }

        // The type id of the component type stored in this registry
        private readonly TypeId m_typeId;

        // Stores the permanent link mapping entity IDs to the actual component data
        private readonly IntSparseMap<EntityComponentLink> m_entityToComponentLinks = new();

        // The array storing actual component data
        private CompactArrayListBase m_components;

        // The array storing components' owning entity's ID
        private readonly CompactArrayList<Entity> m_componentOwners = new();

        // The array storing the compact

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

        private ComponentRegistry(TypeId typeId)
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

        public bool RemoveComponent<T>(Entity entity) where T : struct, IComponent
        {
            Debug.Assert(ComponentTypeId<T>.Value == m_typeId);
            ref var e2CLink = ref m_entityToComponentLinks.TryGetValue(entity.id, out var hasComponent);
            if (!hasComponent)
            {
                return false;
            }

            var componentArray = CastComponentArray<T>();
            componentArray.RemoveAt(e2CLink.componentAddress);
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