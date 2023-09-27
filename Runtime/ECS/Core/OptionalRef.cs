using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Puffercat.Uxt.ECS.Core
{
    public readonly ref struct OptionalRef<T>
    {
        private readonly Span<T> m_storage;

        /// <summary>
        /// Creates an optional ref from a ref.
        /// It is the caller's responsibility to ensure that the optional ref cannot
        /// cannot outlive the referred object
        /// </summary>
        /// <param name="value"></param>
        public OptionalRef(ref T value)
        {
            m_storage = MemoryMarshal.CreateSpan(ref value, 1);
        }

        /// <summary>
        /// Creates an optional ref that references the first element in a span (if the span is empty),
        /// or a null ref (if the span is empty).
        /// </summary>
        /// <param name="span"></param>
        public OptionalRef(Span<T> span)
        {
            m_storage = span;
        }
        
        public bool HasValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !m_storage.IsEmpty;
        }

        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_storage[0];
        }
    }
}