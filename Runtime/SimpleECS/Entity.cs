using System;
using System.Collections.Generic;

namespace Puffercat.Uxt.SimpleECS
{
    public class Entity
    {
        internal int PersistentId { get; }
        public bool PendingDestroy { get; internal set; }
        
        public EntityRegistry Registry { get; }

        internal Entity(EntityRegistry registry, int persistentId)
        {
            Registry = registry;
            PersistentId = persistentId;
        }
        
        private Dictionary<Type, IComponent> m_components;
        
        
    }
}