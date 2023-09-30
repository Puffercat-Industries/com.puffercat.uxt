using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Puffercat.Uxt.ECS.Core
{
    public delegate bool ComponentTypeSetPredicate(ImmutableHashSet<ComponentTypeId> typeIds);
    
    /// <summary>
    /// Represents a predicate function on a set of component types.
    /// This allows a quick filtering of entities by their archetypes.
    /// </summary>
    public struct GroupQuery
    {
        private readonly ComponentTypeSetPredicate m_predicate;

        public GroupQuery(ComponentTypeSetPredicate predicate)
        {
            m_predicate = predicate;
        }
    }

    
    public class GroupQueryBuilder
    {
        private readonly HashSet<ComponentTypeId> m_included = new();
        private readonly HashSet<ComponentTypeId> m_excluded = new();
        
        public GroupQueryBuilder Include<T>() where T : struct, IEntityComponent<T>
        {
            m_included.Add(ComponentTypeId<T>.Value);
            return this;
        }

        public GroupQueryBuilder Exclude<T>() where T : struct, IEntityComponent<T>
        {
            m_excluded.Add(ComponentTypeId<T>.Value);
            return this;
        }

        public GroupQuery Build()
        {
            var included = m_included.ToImmutableHashSet();
            var excluded = m_excluded.ToImmutableHashSet();

            bool Predicate(ImmutableHashSet<ComponentTypeId> typeIds)
            {
                return included.IsSubsetOf(typeIds) &&
                       excluded.All(typeid => !typeIds.Contains(typeid));
            }

            return new GroupQuery(Predicate);
        }
    }
}