using System.Collections.Generic;
using UnityEngine.Pool;

namespace Puffercat.Uxt.ECS.Core
{
    public struct ComponentDestructionCallbackHandle
    {
        // The +1 offset is to make sure that id == -1 represents null. This makes a good default constructor.
        internal readonly int idPlusOne;
        internal readonly uint version;

        internal ComponentDestructionCallbackHandle(int idPlusOne, uint version)
        {
            this.idPlusOne = idPlusOne;
            this.version = version;
        }
    }

    /// <summary>
    /// A table storing the destruction callback for all entity-component pair
    /// </summary>
    internal class ComponentDestructionCallbackTable
    {
        private struct ComponentDestructionCallback_ForwardLink
        {
            public ComponentTypeId typeId;
            public int entityId;
            public int locationInList;
        }

        private struct ComponentDestructionCallback_BackwardLink
        {
            public ComponentDestructionCallback callback;
            public int id;
        }

        private readonly FreeListIntSparseMap<ComponentDestructionCallback_ForwardLink> m_forwardTable =
            new();

        private readonly IntSparseMap<List<ComponentDestructionCallback_BackwardLink>>[] m_backwardTable =
            new IntSparseMap<List<ComponentDestructionCallback_BackwardLink>>[ComponentTypeIdRegistry.MaxNumTypes];

        private readonly IntSparseMap<uint> m_versions =
            new();

        public ComponentDestructionCallbackHandle AddCallback(
            Entity entity,
            ComponentTypeId componentTypeId,
            ComponentDestructionCallback callback)
        {
            var callbackId = m_forwardTable.Add(default);
            ref var forwardLink = ref m_forwardTable.At(callbackId);

            forwardLink.typeId = componentTypeId;
            forwardLink.entityId = entity.id;

            var backwardLink = new ComponentDestructionCallback_BackwardLink
            {
                callback = callback,
                id = callbackId
            };

            var list = GetOrCreateCallbackList(entity.id, componentTypeId);
            forwardLink.locationInList = list.Count;
            list.Add(backwardLink);

            return new ComponentDestructionCallbackHandle(
                callbackId + 1, m_versions.IncrementKey(callbackId));
        }

        public bool RemoveCallback(ComponentDestructionCallbackHandle handle)
        {
            var callbackId = handle.idPlusOne - 1;
            if (callbackId == -1) return false;
            var forwardLink = m_forwardTable.TryGetValue(callbackId, out var found);
            if (!found) return false;
            if (m_versions.At(callbackId) != handle.version) return false;
            m_versions.At(callbackId)++;
            var list = GetOrCreateCallbackList(forwardLink.entityId, forwardLink.typeId);
            var movedBackwardLink = list[forwardLink.locationInList] = list[^1];
            m_forwardTable.At(movedBackwardLink.id).locationInList = forwardLink.locationInList;
            m_forwardTable.Remove(callbackId);
            list.RemoveAt(list.Count - 1);
            if (list.Count == 0)
            {
                m_backwardTable[forwardLink.typeId].Remove(forwardLink.entityId);
                ListPool<ComponentDestructionCallback_BackwardLink>.Release(list);
            }

            return true;
        }

        /// <summary>
        /// Invoke all destruction callbacks on every component-entity pair given in the parameters
        /// {(e0, T), (e1, T), ... (en, T)}, then remove all those callbacks.<br/>
        /// All pairs must have the same component type id. Caller MUST ensure that all entities passed
        /// in are distinct.
        /// </summary>
        /// <param name="componentTypeId">The type of component T in the pair</param>
        /// <param name="entities">All the entities {e0, e1, ..., en}</param>
        /// <remarks>If the entities are sorted by their id, this method might have better cache performance</remarks>
        public void InvokeCallbacks(ComponentTypeId componentTypeId, IEnumerable<Entity> entities)
        {
            // The below two conditionals exists as an optimization. If no destruction callback has ever been registered 
            // for this type of component, then there is no need to iterate.
            // The implication is that you should think carefully about whether you need a certain type of component
            // to have destruction callbacks ever.
            
            if (m_backwardTable[componentTypeId] is not { } componentTable) return;
            if (componentTable.Count == 0) return;

            // Store the entities whose callback list need to be removed (for the said type of component)
            using var _0 = ListPool<int>.Get(out var entityIdsToRemove);

            foreach (var entity in entities)
            {
                var listOpt = componentTable.TryGetValue(entity.id);
                if (listOpt.HasValue)
                {
                    foreach (var callback in listOpt.Value)
                    {
                        callback.callback?.Invoke(entity);
                    }

                    entityIdsToRemove.Add(entity.id);
                }
            }

            // Do the bulk removal of callbacks. We don't need to update forward links into the list
            // since we are removing everything in the list at once.
            foreach (var entityIdToRemove in entityIdsToRemove)
            {
                ref var list = ref componentTable.At(entityIdToRemove);
                foreach (var backwardLink in list)
                {
                    m_versions.At(backwardLink.id)++;
                    m_forwardTable.Remove(backwardLink.id);
                }

                list.Clear();
                ListPool<ComponentDestructionCallback_BackwardLink>.Release(list);
                componentTable.Remove(entityIdToRemove);
            }
        }

        private List<ComponentDestructionCallback_BackwardLink> GetOrCreateCallbackList(
            int entityId, ComponentTypeId componentTypeId)
        {
            if (m_backwardTable[componentTypeId] is not { } componentTable)
            {
                componentTable = new IntSparseMap<List<ComponentDestructionCallback_BackwardLink>>();
                m_backwardTable[componentTypeId] = componentTable;
            }

            List<ComponentDestructionCallback_BackwardLink> list;
            if (componentTable.TryGetValue(entityId) is var listOpt &&
                !listOpt.HasValue)
            {
                list = ListPool<ComponentDestructionCallback_BackwardLink>.Get();
                componentTable.Add(entityId, list);
            }
            else
            {
                list = listOpt.Value;
            }

            return list;
        }
    }
}