using System;
using System.Collections.Generic;

namespace Puffercat.Uxt.SimpleECS
{
    public class Entity
    {
        internal ulong PersistentIdVersion { get; }
        internal int PersistentId { get; }
        public bool PendingDestruction { get; internal set; }

        public EntityRegistry Registry { get; internal set; }

        internal Entity(EntityRegistry registry, int persistentId, ulong persistentIdVersion)
        {
            Registry = registry;
            PersistentId = persistentId;
            PersistentIdVersion = persistentIdVersion;
        }

        private readonly Dictionary<Type, IComponent> m_components = new();

        public bool IsValid() => Registry != null;

        public void Destroy()
        {
            Registry.DestroyEntity(this);
        }

        public bool HasComponent<T>() where T : IComponent
        {
            return TryGetComponent(out T _);
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

        public T GetComponent<T>() where T : class, IComponent
        {
            if (TryGetComponent(out T component))
            {
                return component;
            }

            return null;
        }

        public T AddOrGetComponent<T>() where T : IComponent, new()
        {
            if (TryGetComponent(out T component))
            {
                return component;
            }

            var newComponent = AddComponentUnsafe<T>();
            return newComponent;
        }

        public Entity AddComponent<T>() where T : IComponent, new()
        {
            if (HasComponent<T>())
            {
                throw new Exception($"Component of type {typeof(T)} is already added");
            }

            AddComponentUnsafe<T>();
            return this;
        }

        private T AddComponentUnsafe<T>() where T : IComponent, new()
        {
            var newComponent = new T();
            m_components.Add(typeof(T), newComponent);
            return newComponent;
        }

        public bool RemoveComponent<T>() where T : IComponent
        {
            return m_components.Remove(typeof(T));
        }

        public EntityHandle GetHandle() => new(this);

        public T AddComponent<T>(T component) where T : IComponent
        {
            m_components.Add(component.GetType(), component);
            return component;
        }
    }
}