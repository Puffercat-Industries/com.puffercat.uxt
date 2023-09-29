using System;
using System.Runtime.CompilerServices;

namespace Puffercat.Uxt.ECS.Core
{
    public struct ComponentTypeId<T> where T : struct, IComponent
    {
        public static readonly ComponentTypeId Value;

        static ComponentTypeId()
        {
            Value = ComponentTypeIdRegistry.AllocateTypeId();
        }
    }

    internal static class ComponentTypeIdRegistry
    {
        public const int MaxNumTypes = 512;
        public static int NumAllocatedTypes { get; private set; } = 1;

        public static ComponentTypeId AllocateTypeId()
        {
            if (NumAllocatedTypes >= MaxNumTypes)
            {
                throw new Exception($"You can register at most {MaxNumTypes} types in the type id registry");
            }

            return new ComponentTypeId(NumAllocatedTypes++);
        }
    }
    
    public readonly struct ComponentTypeId : IEquatable<ComponentTypeId>, IComparable<ComponentTypeId>
    {
        private readonly int m_value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentTypeId(int value)
        {
            m_value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeId FromInt(int value)
        {
            return new ComponentTypeId(value);
        }

        public override bool Equals(object obj)
        {
            if (obj is ComponentTypeId)
            {
                return Equals((ComponentTypeId)obj);
            }
            return false;
        }

        public bool Equals(ComponentTypeId other)
        {
            return m_value == other.m_value;
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public static bool operator ==(ComponentTypeId a, ComponentTypeId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ComponentTypeId a, ComponentTypeId b)
        {
            return !a.Equals(b);
        }

        public int CompareTo(ComponentTypeId other)
        {
            return m_value.CompareTo(other.m_value);
        }

        public static bool operator <(ComponentTypeId a, ComponentTypeId b)
        {
            return a.m_value < b.m_value;
        }

        public static bool operator >(ComponentTypeId a, ComponentTypeId b)
        {
            return a.m_value > b.m_value;
        }

        public static bool operator <=(ComponentTypeId a, ComponentTypeId b)
        {
            return a.m_value <= b.m_value;
        }

        public static bool operator >=(ComponentTypeId a, ComponentTypeId b)
        {
            return a.m_value >= b.m_value;
        }

        public override string ToString()
        {
            return m_value.ToString();
        }
        
        public static implicit operator int(in ComponentTypeId typeId)
        {
            return typeId.m_value;
        }
    }

}