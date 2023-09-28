using System;

namespace Puffercat.Uxt.ECS.Core
{
    /// <summary>
    /// An Entity is an object that can store a list of components.
    /// Every entity residing in a registry at a given time has a unique ID.
    /// When an entity is destroyed, its ID can be reused. Therefore, each handle
    /// to an entity has a version associated with it.
    /// </summary>
    public readonly struct Entity : IComparable<Entity>, IComparable, IEquatable<Entity>
    {
        public readonly int id;
        public readonly ulong version;

        internal Entity(int id, ulong version)
        {
            this.id = id;
            this.version = version;
        }
        
        public bool Equals(Entity other)
        {
            return version == other.version && id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is Entity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(version, id);
        }
        
        public int CompareTo(Entity other)
        {
            var idComparison = id.CompareTo(other.id);
            if (idComparison != 0) return idComparison;
            return version.CompareTo(other.version);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is Entity other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Entity)}");
        }

        public static bool operator <(Entity left, Entity right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Entity left, Entity right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Entity left, Entity right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Entity left, Entity right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
    
    public readonly struct EntityComponentLink
    {
        public readonly int componentAddress;
        
        internal EntityComponentLink(int componentAddress)
        {
            this.componentAddress = componentAddress;
        }
    }
}