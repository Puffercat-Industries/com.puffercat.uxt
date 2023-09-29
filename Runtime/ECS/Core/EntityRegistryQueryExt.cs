using System.Collections.Generic;
using UnityEngine.Pool;

namespace Puffercat.Uxt.ECS.Core
{
    public static class EntityRegistryQueryExt
    {
        public static IEnumerable<Entity> GetAllEntitiesWithComponent<T0, T1>(this EntityRegistry entityRegistry)
            where T0 : struct, IEntityComponent<T0>
            where T1 : struct, IEntityComponent<T1>
        {
            using (ListPool<ComponentTypeId>.Get(out var typeList))
            {
                typeList.Add(ComponentTypeId<T0>.Value);
                typeList.Add(ComponentTypeId<T1>.Value);
                foreach (var entity in entityRegistry.GetAllEntitiesWithComponent(typeList))
                {
                    yield return entity;
                }
            }
        }

        public static IEnumerable<Entity> GetAllEntitiesWithComponent<T0, T1, T2>(this EntityRegistry entityRegistry)
            where T0 : struct, IEntityComponent<T0>
            where T1 : struct, IEntityComponent<T1>
            where T2 : struct, IEntityComponent<T2>
        {
            using (ListPool<ComponentTypeId>.Get(out var typeList))
            {
                typeList.Add(ComponentTypeId<T0>.Value);
                typeList.Add(ComponentTypeId<T1>.Value);
                typeList.Add(ComponentTypeId<T2>.Value);
                foreach (var entity in entityRegistry.GetAllEntitiesWithComponent(typeList))
                {
                    yield return entity;
                }
            }
        }

        public static IEnumerable<Entity> GetAllEntitiesWithComponent<T0, T1, T2, T3>(this EntityRegistry entityRegistry)
            where T0 : struct, IEntityComponent<T0>
            where T1 : struct, IEntityComponent<T1>
            where T2 : struct, IEntityComponent<T2>
            where T3 : struct, IEntityComponent<T3>
        {
            using (ListPool<ComponentTypeId>.Get(out var typeList))
            {
                typeList.Add(ComponentTypeId<T0>.Value);
                typeList.Add(ComponentTypeId<T1>.Value);
                typeList.Add(ComponentTypeId<T2>.Value);
                typeList.Add(ComponentTypeId<T3>.Value);
                foreach (var entity in entityRegistry.GetAllEntitiesWithComponent(typeList))
                {
                    yield return entity;
                }
            }
        }

        public static IEnumerable<Entity> GetAllEntitiesWithComponent<T0, T1, T2, T3, T4>(this EntityRegistry entityRegistry)
            where T0 : struct, IEntityComponent<T0>
            where T1 : struct, IEntityComponent<T1>
            where T2 : struct, IEntityComponent<T2>
            where T3 : struct, IEntityComponent<T3>
            where T4 : struct, IEntityComponent<T4>
        {
            using (ListPool<ComponentTypeId>.Get(out var typeList))
            {
                typeList.Add(ComponentTypeId<T0>.Value);
                typeList.Add(ComponentTypeId<T1>.Value);
                typeList.Add(ComponentTypeId<T2>.Value);
                typeList.Add(ComponentTypeId<T3>.Value);
                typeList.Add(ComponentTypeId<T4>.Value);
                foreach (var entity in entityRegistry.GetAllEntitiesWithComponent(typeList))
                {
                    yield return entity;
                }
            }
        }


        public static IEnumerable<Entity> GetAllEntitiesWithComponent(this EntityRegistry entityRegistry,
            List<ComponentTypeId> typeIds)
        {
            typeIds.Sort((lhs, rhs) =>
                entityRegistry.CountComponent(lhs).CompareTo(entityRegistry.CountComponent(rhs)));

            foreach (var entity in entityRegistry.GetAllEntitiesWithComponent(typeIds[0]))
            {
                var ok = true;
                for (var i = 1; i < typeIds.Count; ++i)
                {
                    if (entityRegistry.HasComponent(entity, typeIds[i])) continue;
                    ok = false;
                    break;
                }

                if (ok)
                {
                    yield return entity;
                }
            }
        }
    }
}