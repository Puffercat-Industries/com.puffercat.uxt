using System;

namespace Puffercat.Uxt.SimpleECS
{
    public struct EntityHandle
    {
        internal readonly bool isValid;
        internal readonly int persistentId;
        internal readonly ulong version;

        internal EntityHandle(Entity entity)
        {
            isValid = true;
            persistentId = entity.PersistentId;
            version = entity.PersistentIdVersion;
        }

        /// <summary>
        /// Get the entity that this entity handle references
        /// in the entity registry.
        /// </summary>
        /// <param name="registry">The registry that this entity handle was obtained from</param>
        /// <returns></returns>
        public Entity Get(EntityRegistry registry)
        {
            return registry.Get(this);
        }

        public static EntityHandle Null => default;

        public static bool operator ==(in EntityHandle lhs, in EntityHandle rhs)
        {
            return lhs.isValid == rhs.isValid && lhs.persistentId == rhs.persistentId && lhs.version == rhs.version;
        }

        public static bool operator !=(EntityHandle lhs, EntityHandle rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(EntityHandle other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(isValid, persistentId, version);
        }
    }
}