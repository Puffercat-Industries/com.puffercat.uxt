using System.Collections.Generic;
using UnityEngine;

namespace Puffercat.Uxt.SimpleECS
{
    public class EntityRegistry
    {
        private struct PersistentEntityRecord
        {
            public ulong version;
            public int dynamicIdOrPtr;
        }

        private readonly List<PersistentEntityRecord> m_entityRecords = new();
        private readonly List<Entity> m_entities = new();
        private int m_freePtr;

        public Entity Get(in EntityHandle handle)
        {
            Debug.Assert(handle.persistentId >= 0 && handle.persistentId <= m_entityRecords.Count,
                "handle.persistentId >= 0 && handle.persistentId <= m_entityRecords.Count");
            var rec = m_entityRecords[handle.persistentId];

            // Handle is invalid, so returns null
            if (!handle.isValid)
            {
                return null;
            }
            
            // Entity has been destroyed, so return null
            if (handle.version != rec.version)
            {
                return null;
            }

            return m_entities[rec.dynamicIdOrPtr];
        }

        public Entity CreateEntity()
        {
            // Allocates a free record if none exists anymore
            if (m_freePtr == -1)
            {
                m_freePtr = m_entityRecords.Count;
                m_entityRecords.Add(new PersistentEntityRecord
                {
                    version = 0ul,
                    dynamicIdOrPtr = -1
                });
            }

            var recIndex = m_freePtr;
            m_freePtr = m_entityRecords[m_freePtr].dynamicIdOrPtr;
            
            var rec = m_entityRecords[recIndex];
            ++rec.version;
            rec.dynamicIdOrPtr = m_entities.Count;
            var entity = new Entity(this, recIndex);
            m_entities.Add(entity);
            m_entityRecords[recIndex] = rec;

            return entity;
        }

        public void PerformPendingDestruction()
        {
            for (var i = m_entityRecords.Count - 1; i >= 0; --i)
            {
                var entity = m_entities[i];
                if (entity.PendingDestroy)
                {
                    // Move the entity at the tail of the entity list to
                    // the position of the entity that is being destroyed.
                    // Update its persistent record accordingly
                    var lastEntity = m_entities[^1];
                    var lastRec = m_entityRecords[lastEntity.PersistentId];
                    lastRec.dynamicIdOrPtr = i;
                    m_entities[i] = lastEntity;
                    m_entityRecords[lastEntity.PersistentId] = lastRec;
                    
                    m_entities.RemoveAt(m_entities.Count - 1);

                    // Mark the destroyed entity's record as free
                    var persistentId = entity.PersistentId;
                    var rec = m_entityRecords[persistentId];
                    ++rec.version;
                    rec.dynamicIdOrPtr = m_freePtr;
                    m_freePtr = persistentId;
                    m_entityRecords[persistentId] = rec;
                }
            }
        }

        public void DestroyEntity(Entity entity)
        {
            entity.PendingDestroy = false;
        }

        public void DestroyEntity(EntityHandle entityHandle)
        {
            Debug.Assert(entityHandle.isValid, "entityHandle.isValid");
            entityHandle.Get(this).PendingDestroy = true;
        }
    }
}