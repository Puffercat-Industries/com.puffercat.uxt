using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Puffercat.Uxt.ECS.Core
{
    internal static class DummyRef<T>
    {
        private static T s_dummy;

        public static ref T Dummy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref s_dummy;
        }
    }

    internal class IntSparseMap<T>
    {
        private const int PageSizeExp = 9;
        private const int PageSize = 1 << PageSizeExp;
        private const int BitsetSize = 1 << (PageSizeExp - 3);

        private readonly struct Page
        {
            private readonly T[] m_array;
            private readonly byte[] m_bitset;

            public ref T this[int index] => ref m_array[index];
            public readonly bool IsEmpty => m_array == null;

            private Page(int arraySize, int bitsetSize)
            {
                m_array = new T[arraySize];
                m_bitset = new byte[bitsetSize];
            }

            public static Page Create()
            {
                return new Page(PageSize, BitsetSize);
            }

            public void SetBit(int index)
            {
                var byteIndex = index >> 3;
                var offsetInByte = index & 7;
                m_bitset[byteIndex] = (byte)(m_bitset[byteIndex] | (1 << offsetInByte));
            }

            public void ClearBit(int index)
            {
                var byteIndex = index >> 3;
                var offsetInByte = index & 7;
                m_bitset[byteIndex] = (byte)(m_bitset[byteIndex] & ~(1 << offsetInByte));
            }

            public readonly bool CheckBit(int index)
            {
                var byteIndex = index >> 3;
                var offsetInByte = index & 7;
                return (m_bitset[byteIndex] & (1 << offsetInByte)) != 0;
            }
        }

        private readonly List<Page> m_pages = new();

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }
        
        public ref T At(int key)
        {
            BreakIndex(key, out var pageIndex, out var offsetInPage);

            Debug.Assert(
                pageIndex < m_pages.Count &&
                !m_pages[pageIndex].IsEmpty &&
                m_pages[pageIndex].CheckBit(offsetInPage),
                "Invalid key");

            return ref m_pages[pageIndex][offsetInPage];
        }

        public ref T TryGetValue(int key, out bool found)
        {
            BreakIndex(key, out var pageIndex, out var offsetInPage);

            if (pageIndex >= m_pages.Count)
            {
                found = false;
                return ref DummyRef<T>.Dummy;
            }

            var page = m_pages[pageIndex];

            if (page.IsEmpty || !page.CheckBit(offsetInPage))
            {
                found = false;
                return ref DummyRef<T>.Dummy;
            }

            found = true;
            return ref page[offsetInPage];
        }

        public OptionalRef<T> TryGetValue(int key)
        {
            ref var value = ref TryGetValue(key, out var found);
            return found ? new OptionalRef<T>(ref value) : default;
        }

        public bool ContainsKey(int key)
        {
            BreakIndex(key, out var pageIndex, out var offsetInPage);

            if (pageIndex >= m_pages.Count)
            {
                return false;
            }

            var page = m_pages[pageIndex];

            return !page.IsEmpty && page.CheckBit(offsetInPage);
        }

        public ref T Add(int key, T value)
        {
            BreakIndex(key, out var pageIndex, out var offsetInPage);

            while (m_pages.Count <= pageIndex)
            {
                m_pages.Add(default);
            }

            if (m_pages[pageIndex].IsEmpty)
            {
                m_pages[pageIndex] = Page.Create();
            }

            var page = m_pages[pageIndex];
            if (page.CheckBit(offsetInPage))
            {
                throw new Exception($"{key} already in the sparse map!");
            }

            page[offsetInPage] = value;
            page.SetBit(offsetInPage);
            Count++;
            return ref page[offsetInPage];
        }

        public bool Remove(int key)
        {
            BreakIndex(key, out var pageIndex, out var offsetInPage);

            if (pageIndex >= m_pages.Count)
            {
                return false;
            }

            var page = m_pages[pageIndex];

            if (page.IsEmpty || !page.CheckBit(offsetInPage))
            {
                return false;
            }

            page.ClearBit(offsetInPage);
            Count--;
            return true;
        }

        private static void BreakIndex(int key, out int pageIndex, out int offsetInPage)
        {
            Debug.Assert(key >= 0, "Negative key not supported");
            pageIndex = key >> PageSizeExp;
            offsetInPage = key & (PageSize - 1);
        }
    }

    /// <summary>
    /// The FreeListIntSparseMap manages an internal IntSparseMap. It allows you to quickly find
    /// unused keys in the map with the help of a free-list data structure. However,
    /// it does not permit manual specification of keys for additions. Keys are managed internally
    /// to ensure efficient reuse and avoid collisions.
    /// </summary>
    internal class FreeListIntSparseMap<T>
    {
        private readonly IntSparseMap<T> m_internalSparseMap = new();

        private const int FreeListGrowStep = 128;

        private readonly List<int> m_freeList = new();
        private int m_firstFree = -1;

        private void GrowFreeList()
        {
            var newFirstFree = m_freeList.Count;
            m_freeList.AddRange(Enumerable.Range(m_freeList.Count + 1, FreeListGrowStep));
            m_freeList[^1] = m_firstFree;
            m_firstFree = newFirstFree;
        }

        private int AllocateAvailableKey()
        {
            if (m_firstFree == -1)
            {
                GrowFreeList();
            }

            var allocatedKey = m_firstFree;
            m_firstFree = m_freeList[allocatedKey];
            m_freeList[allocatedKey] = -2;
            return allocatedKey;
        }

        public int Add(T value)
        {
            var key = AllocateAvailableKey();
            m_internalSparseMap.Add(key, value);
            return key;
        }

        public ref T At(int key)
        {
            return ref m_internalSparseMap.At(key);
        }

        public ref T TryGetValue(int key, out bool found)
        {
            return ref m_internalSparseMap.TryGetValue(key, out found);
        }

        public bool Remove(int key)
        {
            var removed = m_internalSparseMap.Remove(key);

            if (removed)
            {
                Debug.Assert(m_freeList[key] == -2);
                m_freeList[key] = m_firstFree;
                m_firstFree = key;
            }

            return removed;
        }
    }

    internal static class IntSparseMapExt
    {
        internal static uint IncrementKey(this IntSparseMap<uint> map, int key)
        {
            var value = map.TryGetValue(key);
            if (value.HasValue)
            {
                return ++value.Value;
            }
            else
            {
                map.Add(key, 1);
                return 1;
            }
        }

        internal static ulong IncrementKey(this IntSparseMap<ulong> map, int key)
        {
            var value = map.TryGetValue(key);
            if (value.HasValue)
            {
                return ++value.Value;
            }
            else
            {
                map.Add(key, 1);
                return 1;
            }
        }
    }
}