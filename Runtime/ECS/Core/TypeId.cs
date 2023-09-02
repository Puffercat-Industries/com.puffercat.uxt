using System;

namespace Puffercat.Uxt.ECS.Core
{
    public struct TypeId<T> where T : struct, IComponent
    {
        public static readonly int Value;

        static TypeId()
        {
            Value = TypeIdRegistry.AllocateTypeId();
        }
    }

    public static class TypeIdRegistry
    {
        public const int MaxNumTypes = 512;
        public static int NumAllocatedTypes { get; private set; }

        public static int AllocateTypeId()
        {
            if (NumAllocatedTypes >= MaxNumTypes)
            {
                throw new Exception($"You can register at most {MaxNumTypes} types in the type id registry");
            }

            return NumAllocatedTypes++;
        }
    }
}