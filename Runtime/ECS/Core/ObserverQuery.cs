using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Puffercat.Uxt.Algorithms;

namespace Puffercat.Uxt.ECS.Core
{
    public enum ComponentBehaviourEvent
    {
        AddedOrModified,
        Removed
    }

    public readonly struct ComponentBehaviourQuery : IEquatable<ComponentBehaviourQuery>,
        IComparable<ComponentBehaviourQuery>, IComparable
    {
        public int CompareTo(ComponentBehaviourQuery other)
        {
            var typeIdComparison = typeId.CompareTo(other.typeId);
            if (typeIdComparison != 0) return typeIdComparison;
            return behaviourEvent.CompareTo(other.behaviourEvent);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is ComponentBehaviourQuery other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(ComponentBehaviourQuery)}");
        }

        public static bool operator <(ComponentBehaviourQuery left, ComponentBehaviourQuery right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(ComponentBehaviourQuery left, ComponentBehaviourQuery right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(ComponentBehaviourQuery left, ComponentBehaviourQuery right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(ComponentBehaviourQuery left, ComponentBehaviourQuery right)
        {
            return left.CompareTo(right) >= 0;
        }

        public readonly ComponentTypeId typeId;
        public readonly ComponentBehaviourEvent behaviourEvent;

        public bool Equals(ComponentBehaviourQuery other)
        {
            throw new System.NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentBehaviourQuery other && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new System.NotImplementedException();
        }

        public static bool operator ==(ComponentBehaviourQuery left, ComponentBehaviourQuery right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentBehaviourQuery left, ComponentBehaviourQuery right)
        {
            return !left.Equals(right);
        }
    }

    public class ObserverQuery
    {
        private readonly GroupQuery m_groupQuery;
        private readonly List<ComponentBehaviourQuery> m_behaviourQueries;

        internal ObserverQuery(GroupQuery groupQuery, IEnumerable<ComponentBehaviourQuery> behaviourQueries)
        {
            m_groupQuery = groupQuery;
            m_behaviourQueries = behaviourQueries.ToList();
            m_behaviourQueries.Sort();
            m_behaviourQueries.RemoveRange(m_behaviourQueries.DistinctRange(0, m_behaviourQueries.Count));
        }
    }
}