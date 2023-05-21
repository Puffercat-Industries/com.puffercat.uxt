using System;
using System.Collections.Generic;

namespace Puffercat.Uxt.Containers
{
    public class SubscriberMap<TKey, TSubscriber>
    {
        private readonly Dictionary<TKey, SafeIterationCollection<TSubscriber>> m_subscribers = new();

        public struct Handle : IDisposable
        {
            private SafeIterationCollection<TSubscriber>.Handle m_internalHandle;

            internal Handle(SafeIterationCollection<TSubscriber>.Handle internalHandle)
            {
                m_internalHandle = internalHandle;
            }

            public bool IsValid()
            {
                return m_internalHandle.IsValid();
            }

            public void Dispose()
            {
                if (IsValid())
                {
                    m_internalHandle.RemoveFromCollection();
                    m_internalHandle = default;
                }
            }
        }

        public Handle Subscribe(TKey key, TSubscriber subscriber)
        {
            if (!m_subscribers.TryGetValue(key, out var subscriberList))
            {
                subscriberList = new SafeIterationCollection<TSubscriber>();
                m_subscribers.Add(key, subscriberList);
            }

            var handle = new Handle(subscriberList.Add(subscriber));
            return handle;
        }
    }
}