using System;
using System.Collections.Generic;

namespace Puffercat.Uxt.Utils
{
    public static class DictionaryUtilities
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
            where TValue : class
        {
            if (!dict.TryGetValue(key, out var val))
            {
                val = factory();
                dict.Add(key, val);
            }

            return val;
        }
    }
}