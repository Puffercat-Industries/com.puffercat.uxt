using System;
using UnityEngine;

namespace Puffercat.Uxt.ECS.Core
{
    public sealed class EntityRegistry
    {
        public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
        {
            throw new System.NotImplementedException();
        }

        public ref T AddComponent<T>(Entity entity) where T : struct, IComponent
        {
            throw new System.NotImplementedException();
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
        private readonly IntSparseMap<EntityComponentLink> m_entityToComponentLinks;

        // The array storing actual component data
        private readonly CompactArrayListBase m_components;

        // The array storing components' owner's entity ID
        private readonly CompactArrayList<int> m_componentOwnerIds;

        // The array storing the compact

        // All the component manipulation functions below do not check for whether the entity
        // is alive (i.e. its version is current). This should be guaranteed by the caller!

        public ref T TryGetComponent<T>(Entity entity, out bool success) where T : struct, IComponent
        {
            Debug.Assert(TypeId<T>.Value == m_typeId);
            ref var e2CLink = ref m_entityToComponentLinks.TryGetValue(entity.id, out success);
            if (!success)
            {
                return ref DummyRef<T>.Dummy;
            }

            var componentArray = CastComponentArray<T>();
            ref var component = ref componentArray.At(e2CLink.componentAddress);
            return ref component;
        }

        public ref T AddComponent<T>(Entity entity) where T : struct, IComponent
        {
            Debug.Assert(TypeId<T>.Value == m_typeId);
            if (m_entityToComponentLinks.ContainsKey(entity.id))
            {
                throw new Exception($"Entity {entity.id} already has component {typeof(T)}");
            }

            return ref AddComponentUnsafe<T>(entity);
        }

        public ref T AddOrGetComponent<T>(Entity entity) where T : struct, IComponent
        {
            Debug.Assert(TypeId<T>.Value == m_typeId);
            ref var e2CLink = ref m_entityToComponentLinks.TryGetValue(entity.id, out var success);
            if (success)
            {
                var componentArray = CastComponentArray<T>();
                ref var component = ref componentArray.At(e2CLink.componentAddress);
                return ref component;
            }
            else
            {
                return ref AddComponentUnsafe<T>(entity);
            }
        }

        private ref T AddComponentUnsafe<T>(Entity entity) where T : struct, IComponent
        {
            var componentArray = CastComponentArray<T>();
            var componentAddress = componentArray.Count;
            m_entityToComponentLinks.Add(entity.id, new EntityComponentLink(componentAddress));
            m_componentOwnerIds.Add(entity.id);
            return ref componentArray.Add(default);
        }

        public bool RemoveComponent<T>(Entity entity) where T : struct, IComponent
        {
            Debug.Assert(TypeId<T>.Value == m_typeId);
            ref var e2CLink = ref m_entityToComponentLinks.TryGetValue(entity.id, out var hasComponent);
            if (!hasComponent)
            {
                return false;
            }
            var componentArray = CastComponentArray<T>();
            componentArray.RemoveAt(e2CLink.componentAddress);
            m_componentOwnerIds.RemoveAt(e2CLink.componentAddress);
            m_entityToComponentLinks.Remove(entity.id);
            
            return true;
        }

        private CompactArrayList<T> CastComponentArray<T>() where T : struct, IComponent
        {
            return (CompactArrayList<T>)m_components;
        }
    }
}