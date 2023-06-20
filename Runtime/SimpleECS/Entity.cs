using System;
using System.Collections.Generic;

namespace Puffercat.Uxt.SimpleECS
{
    public class Entity
    {
        internal int PersistentId { get; }
        public bool PendingDestruction { get; internal set; }

        public EntityRegistry Registry { get; internal set; }

        internal Entity(EntityRegistry registry, int persistentId)
        {
            Registry = registry;
            PersistentId = persistentId;
        }

        private Dictionary<Type, IComponent> m_components;

        public bool IsValid() => Registry != null;

        public void Destroy()
        {
            Registry.DestroyEntity(this);
        }

        public bool TryGetComponent<T>(out T outComponent) where T : IComponent
        {
            if (m_components.TryGetValue(typeof(T), out var component))
            {
                outComponent = (T)component;
                return true;
            }

            outComponent = default;
            return false;
        }

        public T AddOrGetComponent<T>() where T : IComponent, new()
        {
            if (TryGetComponent(out T component))
            {
                return component;
            }

            var newComponent = new T();
            m_components.Add(typeof(T), newComponent);
            return newComponent;
        }

        public void RemoveComponent<T>() where T : IComponent
        {
            m_components.Remove(typeof(T));
        }
    }
}