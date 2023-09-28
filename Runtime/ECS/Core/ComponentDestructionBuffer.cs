using System.Collections.Generic;
using Puffercat.Uxt.Algorithms;
using UnityEngine.Pool;

namespace Puffercat.Uxt.ECS.Core
{
    internal class ComponentDestructionBuffer
    {
        // m_buffer[typeId] is a list of entities that need to have component of said typeId removed
        private readonly Dictionary<int, List<Entity>> m_buffer = new();

        /// <summary>
        /// Queues a component for destruction. This function does not check whether
        /// the entity has the component or not. It's the caller's responsibility to make
        /// sure that the entity has this component. Duplicate calls to this function is safe.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeId"></param>
        public void QueueDestructionUnchecked(Entity entity, ComponentTypeId typeId)
        {
            if (!m_buffer.TryGetValue(typeId, out var list))
            {
                list = ListPool<Entity>.Get();
                m_buffer.Add(typeId, list);
            }

            list.Add(entity);
        }

        public void Clear()
        {
            foreach (var list in m_buffer.Values)
            {
                list.Clear();
                ListPool<Entity>.Release(list);
            }

            m_buffer.Clear();
        }

        public void SortAndRemoveDuplicates()
        {
            foreach (var list in m_buffer.Values)
            {
                list.Sort();
                list.RemoveRange(list.DistinctRange(0, list.Count));
            }
        }
    }
}