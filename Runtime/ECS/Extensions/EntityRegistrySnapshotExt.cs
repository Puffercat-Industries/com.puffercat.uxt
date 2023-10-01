using System;
using System.Collections.Generic;
using System.Linq;
using Puffercat.Uxt.ECS.Components;
using Puffercat.Uxt.ECS.Core;

namespace Puffercat.Uxt.ECS.Extensions
{
    public static class EntityRegistrySnapshotExt
    {
        private struct SnapshotSourceComponent : IEntityTag<SnapshotSourceComponent>
        {
        }

        private struct SnapshotComponent : IEntityTag<SnapshotComponent>
        {
        }

        public static Entity CreateEntityWithSnapshotSupport(this EntityRegistry registry)
        {
            var entity = registry.CreateEntity();
            registry.AddOrGetComponent<SnapshotSourceComponent>(entity);
            return entity;
        }

        public static void MakeSnapshot(this EntityRegistry registry)
        {
            registry.ProcessDestruction();
            
            if (registry.GetAllEntitiesWithComponent<SnapshotComponent>().Any())
            {
                throw new Exception("A snapshot is already present. Can't make a second one.");
            }
            
            foreach (var snapshotSourceEntity in registry.GetAllEntitiesWithComponent<SnapshotSourceComponent>())
            {
                var snapshotEntity = registry.CopyEntity(snapshotSourceEntity);
                registry.AddOrGetComponent<SnapshotComponent>(snapshotEntity);
                registry.MarkComponentForRemoval<SnapshotSourceComponent>(snapshotEntity);
            }
            
            registry.ProcessDestruction();
        }

        public static void RestoreSnapshot(this EntityRegistry registry)
        {
            foreach (var snapshotSourceEntity in registry.GetAllEntitiesWithComponent<SnapshotSourceComponent>())
            {
                registry.MarkEntityForDestruction(snapshotSourceEntity);
            }
            
            registry.ProcessDestruction();
            
            foreach (var snapShotEntity in registry.GetAllEntitiesWithComponent<SnapshotComponent>())
            {
                var restoredEntity = registry.CopyEntity(snapShotEntity);
                registry.AddOrGetComponent<SnapshotSourceComponent>(restoredEntity);
                registry.MarkComponentForRemoval<SnapshotComponent>(restoredEntity);
                registry.MarkEntityForDestruction(snapShotEntity);
            }
            
            registry.ProcessDestruction();
        }

        public static IEnumerable<Entity> GetSnapshotSourceEntities<T0>(this EntityRegistry registry)
            where T0 : struct, IEntityComponent<T0>
        {
            return registry.GetAllEntitiesWithComponent<SnapshotSourceComponent, T0>();
        }
        
        public static IEnumerable<Entity> GetSnapshotSourceEntities<T0, T1>(this EntityRegistry registry)
            where T0 : struct, IEntityComponent<T0>
            where T1 : struct, IEntityComponent<T1>
        {
            return registry.GetAllEntitiesWithComponent<SnapshotSourceComponent, T0, T1>();
        }
        
        public static IEnumerable<Entity> GetSnapshotSourceEntities<T0, T1, T2>(this EntityRegistry registry)
            where T0 : struct, IEntityComponent<T0>
            where T1 : struct, IEntityComponent<T1>
            where T2 : struct, IEntityComponent<T2>
        {
            return registry.GetAllEntitiesWithComponent<SnapshotSourceComponent, T0, T1, T2>();
        }
    }
}