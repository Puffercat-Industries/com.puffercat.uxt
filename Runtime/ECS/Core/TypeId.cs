using System;

namespace Puffercat.Uxt.ECS.Core
{
    public struct TypeId<T> where T : struct, IComponent
    {
        public static readonly TypeId Value;

        static TypeId()
        {
            Value = TypeIdRegistry.AllocateTypeId();
        }
    }

    public static class TypeIdRegistry
    {
        public const int MaxNumTypes = 512;
        public static int NumAllocatedTypes { get; private set; } = 1;

        public static TypeId AllocateTypeId()
        {
            if (NumAllocatedTypes >= MaxNumTypes)
            {
                throw new Exception($"You can register at most {MaxNumTypes} types in the type id registry");
            }

            return new TypeId(NumAllocatedTypes++);
        }
    }
    
    public readonly struct TypeId : IEquatable<TypeId>, IComparable<TypeId>
    {
        private readonly int m_value;

        internal TypeId(int value)
        {
            m_value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeId)
            {
                return Equals((TypeId)obj);
            }
            return false;
        }

        public bool Equals(TypeId other)
        {
            return m_value == other.m_value;
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public static bool operator ==(TypeId a, TypeId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(TypeId a, TypeId b)
        {
            return !a.Equals(b);
        }

        public int CompareTo(TypeId other)
        {
            return m_value.CompareTo(other.m_value);
        }

        public static bool operator <(TypeId a, TypeId b)
        {
            return a.m_value < b.m_value;
        }

        public static bool operator >(TypeId a, TypeId b)
        {
            return a.m_value > b.m_value;
        }

        public static bool operator <=(TypeId a, TypeId b)
        {
            return a.m_value <= b.m_value;
        }

        public static bool operator >=(TypeId a, TypeId b)
        {
            return a.m_value >= b.m_value;
        }

        public override string ToString()
        {
            return m_value.ToString();
        }
        
        public static implicit operator int(in TypeId typeId)
        {
            return typeId.m_value;
        }
    }

}