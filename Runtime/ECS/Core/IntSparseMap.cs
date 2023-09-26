using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Puffercat.Uxt.ECS.Core
{
    public static class DummyRef<T>
    {
        private static T s_dummy = default;

        public static ref T Dummy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref s_dummy;
        }
    }

    public class IntSparseMap<T>
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

        private readonly List<Page> m_pages;

        public IntSparseMap()
        {
            m_pages = new List<Page>();
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

        public void Add(int key, T value)
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
            return true;
        }

        private static void BreakIndex(int key, out int pageIndex, out int offsetInPage)
        {
            Debug.Assert(key >= 0, "Negative key not supported");
            pageIndex = key >> PageSizeExp;
            offsetInPage = key & (PageSize - 1);
        }
    }
}