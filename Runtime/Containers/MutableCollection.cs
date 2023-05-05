using System;
using System.Collections;
using System.Collections.Generic;

namespace Puffercat.Uxt.Containers
{
    /// <summary>
    /// A collection that supports removal and insertion of objects during iteration.
    /// Insertion returns a handle that allows later removal of the inserted object from the
    /// collection.
    /// 
    /// Order of iteration is not guaranteed.
    ///
    /// This container is useful for storing a collection of observers in the observer pattern.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    public class MutableCollection<T> : IEnumerable<T>
    {
        internal class ListNode
        {
            public int positionInList;
            public readonly T value;

            public ListNode(int positionInList, T value)
            {
                this.positionInList = positionInList;
                this.value = value;
            }
        }

        public struct Handle
        {
            internal MutableCollection<T> collection;
            internal ListNode node;

            internal Handle(MutableCollection<T> collection, ListNode node)
            {
                this.collection = collection;
                this.node = node;
            }

            public bool IsValid() => node is not null && node.positionInList != -1;

            public void RemoveFromCollection()
            {
                collection.Remove(this);
            }
        }

        private struct Enumerator : IEnumerator<T>
        {
            private readonly MutableCollection<T> m_parent;
            private bool m_disposed;

            public Enumerator(MutableCollection<T> parent) : this()
            {
                m_parent = parent;
            }

            public bool MoveNext()
            {
                if (m_parent.m_enumeratorIndex + 1 >= m_parent.m_list.Count)
                {
                    return false;
                }

                ++m_parent.m_enumeratorIndex;
                Current = m_parent.m_list[m_parent.m_enumeratorIndex].value;
                return true;
            }
            
            void IEnumerator.Reset()
            {
                m_parent.m_enumeratorIndex = -1;
                Current = default;
            }

            public T Current { get; private set; }
            
            object IEnumerator.Current => Current;

            public void Dispose()
            {
                if (!m_disposed)
                {
                    m_disposed = true;
                    m_parent.m_hasEnumerator = false;
                }
            }
        }

        [Serializable]
        private class MultipleEnumerationException : Exception
        {
            public MultipleEnumerationException() : base(
                $"Multiple enumerations of {typeof(MutableCollection<T>)} is not allowed.")
            {
            }
        }

        private readonly List<ListNode> m_list = new();

        private int m_enumeratorIndex;
        private bool m_hasEnumerator = false;

        public Handle Add(T item)
        {
            var node = new ListNode(m_list.Count, item);
            m_list.Add(node);
            return new Handle(this, node);
        }

        private void Remove(Handle handle)
        {
            var removePos = handle.node.positionInList;
            var lastPos = m_list.Count - 1;

            if (!m_hasEnumerator || removePos > m_enumeratorIndex)
            {
                // Simple case: we are not iterating, or we've not yet iterated the element to be removed
                // We can just swap it with last element and remove it.

                (m_list[removePos], m_list[lastPos]) = (m_list[lastPos], m_list[removePos]);
                m_list[removePos].positionInList = removePos;
            }
            else
            {
                // The element to be removed has already been iterated over, so if we just swap it with the last element
                // the last element will be missed by this iteration.
                // Solution: move the last element to the element most recently iterated.

                var iterPos = m_enumeratorIndex;
                --m_enumeratorIndex;

                (m_list[removePos], m_list[iterPos], m_list[lastPos]) =
                    (m_list[iterPos], m_list[lastPos], m_list[removePos]);
            }

            m_list.RemoveAt(m_list.Count - 1);
            handle.node.positionInList = -1;
            handle.node = null;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (m_hasEnumerator)
            {
                throw new MultipleEnumerationException();
            }

            m_hasEnumerator = true;
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}