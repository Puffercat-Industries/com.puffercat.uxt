using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public sealed class AssociativeCounter<TKey> : IReadOnlyDictionary<TKey, int>
    {
        private readonly Dictionary<TKey, int> _dict = new Dictionary<TKey, int>();

        public AssociativeCounter()
        {
        }

        public AssociativeCounter(AssociativeCounter<TKey> other)
        {
            _dict = other._dict.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public int IncrementKey(TKey key)
        {
            if (_dict.ContainsKey(key))
            {
                return ++_dict[key];
            }

            _dict[key] = 1;
            return 1;
        }

        public bool DecrementKey(TKey key)
        {
            if (!_dict.TryGetValue(key, out var count))
            {
                throw new Exception("Could not decrement a key that doesn't exist in the counter");
            }

            if (--_dict[key] == 0)
            {
                _dict.Remove(key);
                return true;
            }

            return false;
        }

        public void SetKey(TKey key, int value)
        {
            Debug.Assert(value >= 0);
            if (value == 0)
            {
                _dict.Remove(key);
            }
            else
            {
                _dict[key] = value;
            }
        }

        public int CountKey(TKey key)
        {
            if (_dict.TryGetValue(key, out var count))
            {
                return count;
            }

            return 0;
        }

        public IEnumerator<KeyValuePair<TKey, int>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_dict).GetEnumerator();
        }

        public int Count => _dict.Count;

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out int value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public int this[TKey key] => _dict[key];

        public IEnumerable<TKey> Keys => _dict.Keys;

        public IEnumerable<int> Values => _dict.Values;
    }
}