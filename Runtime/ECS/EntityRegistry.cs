using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Puffercat.Uxt.ECS
{
    public sealed class EntityRegistry
    {
        private struct PersistentEntityRecord
        {
            public ulong version;
            public int dynamicIdOrPtr;
        }

        private readonly List<PersistentEntityRecord> m_entityRecords = new();
        private readonly List<Entity> m_entities = new();
        private int m_freePtr = -1;

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
            var entity = new Entity(this, recIndex, rec.version);
            m_entities.Add(entity);
            m_entityRecords[recIndex] = rec;

            return entity;
        }

        public EntityRegistry(int initialCapacity = 0)
        {
            Debug.Assert(initialCapacity >= 0, "initialCapacity >= 0");

            m_entityRecords = new List<PersistentEntityRecord>(initialCapacity);
            m_entities = new List<Entity>(initialCapacity);

            for (var i = initialCapacity - 1; i >= 0; --i)
            {
                m_entityRecords.Add(new PersistentEntityRecord
                {
                    version = 0ul,
                    dynamicIdOrPtr = m_freePtr
                });

                m_freePtr = i;
            }
        }

        public bool HandleIsValid(in EntityHandle handle)
        {
            return handle.isValid && m_entityRecords[handle.persistentId].version == handle.version;
        }

        public void PerformPendingDestruction()
        {
            for (var i = m_entities.Count - 1; i >= 0; --i)
            {
                var entity = m_entities[i];
                if (entity.PendingDestruction)
                {
                    // The destroyed entity shall not be associated with registry anymore
                    entity.Registry = null;

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

        /// <summary>
        /// Marks the entity as pending destruction.
        /// It will be destroyed on the next call of <see cref="PerformPendingDestruction"/>
        /// </summary>
        /// <param name="entity">The entity to destroy</param>
        public void DestroyEntity(Entity entity)
        {
            entity.PendingDestruction = true;
        }

        /// <summary>
        /// Marks the entity as pending destruction.
        /// It will be destroyed on the next call of <see cref="PerformPendingDestruction"/>
        /// </summary>
        /// <param name="entityHandle">The entity to destroy</param>
        public void DestroyEntity(EntityHandle entityHandle)
        {
            Debug.Assert(entityHandle.isValid, "entityHandle.isValid");
            entityHandle.Get(this).PendingDestruction = true;
        }

        public bool ContainsEntityWithComponent<TComponent>() where TComponent : IComponent
        {
            return m_entities.Any(ent => ent.HasComponent<TComponent>());
        }

        #region Iteration Methods

        public IEnumerable<(Entity, T1)> IterateEntities<T1>()
            where T1 : IComponent
        {
            var count = m_entities.Count;
            for (var i = 0; i != count; ++i)
            {
                var entity = m_entities[i];
                if (entity.TryGetComponent(out T1 t1))
                {
                    yield return (entity, t1);
                }
            }
        }

        public IEnumerable<(Entity, T1, T2)> IterateEntities<T1, T2>()
            where T1 : IComponent
            where T2 : IComponent
        {
            var count = m_entities.Count;
            for (var i = 0; i != count; ++i)
            {
                var entity = m_entities[i];
                if (entity.TryGetComponent(out T1 t1) && entity.TryGetComponent(out T2 t2))
                {
                    yield return (entity, t1, t2);
                }
            }
        }

        public IEnumerable<(Entity, T1, T2, T3)> IterateEntities<T1, T2, T3>()
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
        {
            var count = m_entities.Count;
            for (var i = 0; i != count; ++i)
            {
                var entity = m_entities[i];
                if (entity.TryGetComponent(out T1 t1) && entity.TryGetComponent(out T2 t2) &&
                    entity.TryGetComponent(out T3 t3))
                {
                    yield return (entity, t1, t2, t3);
                }
            }
        }

        public IEnumerable<(Entity, T1, T2, T3, T4)> IterateEntities<T1, T2, T3, T4>()
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
        {
            var count = m_entities.Count;
            for (var i = 0; i != count; ++i)
            {
                var entity = m_entities[i];
                if (entity.TryGetComponent(out T1 t1) && entity.TryGetComponent(out T2 t2) &&
                    entity.TryGetComponent(out T3 t3) && entity.TryGetComponent(out T4 t4))
                {
                    yield return (entity, t1, t2, t3, t4);
                }
            }
        }

        public IEnumerable<(Entity, T1, T2, T3, T4, T5)> IterateEntities<T1, T2, T3, T4, T5>()
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
            where T5 : IComponent
        {
            var count = m_entities.Count;
            for (var i = 0; i != count; ++i)
            {
                var entity = m_entities[i];
                if (entity.TryGetComponent(out T1 t1) && entity.TryGetComponent(out T2 t2) &&
                    entity.TryGetComponent(out T3 t3) && entity.TryGetComponent(out T4 t4) &&
                    entity.TryGetComponent(out T5 t5))
                {
                    yield return (entity, t1, t2, t3, t4, t5);
                }
            }
        }

        #endregion

        public EntityHandle FindEntityWithComponent<TComponent>() where TComponent : IComponent
        {
            var count = m_entities.Count;
            for (var i = 0; i != count; ++i)
            {
                var entity = m_entities[i];
                if (entity.HasComponent<TComponent>())
                {
                    return entity.GetHandle();
                }
            }

            return EntityHandle.Null;
        }
    }
}