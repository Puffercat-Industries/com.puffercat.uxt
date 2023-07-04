using System;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    [Serializable]
    public struct Optional<T>
    {
        [SerializeField] private bool m_hasValue;
        [SerializeField] private T m_value;

        public bool HasValue => m_hasValue;
        public T Value => m_hasValue ? m_value : throw new NullReferenceException();

        public T ValueOr(T fallback)
        {
            return HasValue ? m_value : fallback;
        }
        
        public static string GetPath_HasValue()
        {
            return nameof(m_hasValue);
        }

        public static string GetPath_Value()
        {
            return nameof(m_value);
        }

        public bool TryGetValue(out T value)
        {
            if (m_hasValue)
            {
                value = m_value;
                return true;
            }

            value = default;
            return false;
        }
    }
}