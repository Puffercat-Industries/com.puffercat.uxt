using System.Collections.Generic;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public static class ListUtility
    {
        public static void Shuffle<T>(this List<T> list)
        {
            for (var dest = 0; dest < list.Count - 1; ++dest)
            {
                var src = Random.Range(dest + 1, list.Count);
                (list[src], list[dest]) = (list[dest], list[src]);
            }
        }

        public static List<T> MakeEmptyList<T>(int size)
        {
            Debug.Assert(size >= 0);
            var list = new List<T>(size);
            for (var i = 0; i < size; ++i)
            {
                list.Add(default);
            }

            return list;
        }
    }
}