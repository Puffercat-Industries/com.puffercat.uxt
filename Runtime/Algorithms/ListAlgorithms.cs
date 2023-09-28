using System;
using System.Collections.Generic;

namespace Puffercat.Uxt.Algorithms
{
    public static class ListAlgorithms
    {
        /// <summary>
        /// Partition the sorted range [start, end) so that the first occurence of
        /// any distinct item is on the left (in order), and the duplicated
        /// are on the right (can be out of order). Returns the start of the
        /// right partition. Similar to C++'s std::unique.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static int DistinctRange<T>(
            this List<T> list,
            int start, 
            int end)
        {
            return DistinctRange(list, start, end, EqualityComparer<T>.Default);
        }
        
        public static int DistinctRange<T>(
            this List<T> list,
            int start, 
            int end,
            IEqualityComparer<T> comparer)
        {
            if (start < 0 || end > list.Count || start >= end)
            {
                throw new ArgumentException("Invalid range");
            }

            if (start == end)
            {
                return start;
            }
            
            ++start;

            for (var i = start; i != end; ++i)
            {
                if (!comparer.Equals(list[start - 1], list[i]))
                {
                    (list[start], list[i]) = (list[i], list[start]);
                    ++start;
                }
            }

            return start;
        }

        public static void RemoveRange<T>(this List<T> list, int start)
        {
            if (start >= list.Count) return;
            list.RemoveRange(start, list.Count - start);
        }
    }
}