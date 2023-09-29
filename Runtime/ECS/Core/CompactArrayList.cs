using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Puffercat.Uxt.ECS.Core
{
    public abstract class CompactArrayListBase
    {
        protected int m_count = 0;
        protected int m_capacity = 0;

        /// <summary>
        /// The number of elements in the list.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_count;
        }


        /// <summary>
        /// Removes the element at the specified index from the list.
        /// Note that this operation does not guarantee the order of other items in the list.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the index is less than 0 or greater than or equal to the count of elements in the list.
        /// </exception>
        public abstract void RemoveAt(int index);
    }

    public sealed class CompactArrayList<T> : CompactArrayListBase
    {
        private const int BlockSizeExp = 7;
        private const int BlockSize = (1 << BlockSizeExp);

        private readonly struct Block
        {
            public readonly T[] storage;

            public Block(int size = BlockSize)
            {
                storage = new T[size];
            }
        }

        private readonly List<Block> m_blocks = new();

        /// <summary>
        /// Retrieves a reference to the element at a specified index in the list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>A reference to the element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the index is less than 0 or greater than or equal to the count of elements in the list.
        /// </exception>
        public ref T At(int index)
        {
            if (index < 0 || index >= m_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return ref AtUnchecked(index);
        }

        /// <summary>
        /// Same as <see cref="CompactArrayList{T}.At"/>, but does not check out-of-bound access
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T AtUnchecked(int index)
        {
            var blockIndex = index >> BlockSizeExp;
            var offset = index & (BlockSize - 1);
            return ref m_blocks[blockIndex].storage[offset];
        }

        /// <summary>
        /// Adds an item to the end of the list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>A reference to the added item.</returns>
        public ref T Add(T item)
        {
            if (m_count == m_capacity)
            {
                m_blocks.Add(new Block(BlockSize));
                m_capacity += BlockSize;
            }

            ref var result = ref AtUnchecked(m_count++);
            result = item;
            return ref result;
        }

        /// <summary>
        /// Removes the element at the specified index from the list.
        /// Note that this operation does not guarantee the order of other items in the list.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the index is less than 0 or greater than or equal to the count of elements in the list.
        /// </exception>
        public override void RemoveAt(int index)
        {
            if (index < 0 || index >= m_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            AtUnchecked(index) = AtUnchecked(m_count - 1);
            AtUnchecked(m_count - 1) = default;
            --m_count;
        }
    }
}