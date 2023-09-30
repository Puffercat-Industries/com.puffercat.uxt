using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Puffercat.Uxt.ECS.Core
{
    /// <summary>
    /// An archetype describes a set of components an entity can have.
    /// Two entities having the same set of components belong to the same archetype.
    ///
    /// The existence make a lot of operations easier, for example:
    ///     - Finding all the components of an entity
    ///     - (This is not implemented yet) Another efficient option for doing group query
    /// </summary>
    public class Archetype
    {
        public const short ErrorArchetypeId = 0;
        public const short EmptyArchetypeId = 1;

        private readonly List<ComponentTypeId> m_typeIds;
        private readonly int m_hashCodeCache;

        /// <summary>
        /// Check if this archetype represents an error.
        ///
        /// <remarks>
        /// The error archetype is a dummy archetype used to detect erroneous operations,
        /// for example: adding duplicate components, removing non-existent components, etc.
        /// </remarks>
        /// 
        /// </summary>
        public bool IsErrorArchetype
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_typeIds is null;
        }

        public IReadOnlyList<ComponentTypeId> ComponentTypes => m_typeIds;

        internal Archetype([NotNull] IEnumerable<ComponentTypeId> componentTypes)
        {
            var list = componentTypes.ToList();
            if (list.Distinct().Count() != list.Count)
            {
                m_typeIds = null;
                m_hashCodeCache = 0;
                return;
            }

            // Sorting is necessary to make archetypes having the same set of components compare equal 
            // regardless of component order.
            list.Sort();

            var hash = -1;
            unchecked
            {
                foreach (var typeId in list)
                {
                    hash = HashCode.Combine(hash, typeId);
                }
            }

            m_hashCodeCache = hash;
            m_typeIds = list;
        }

        internal Archetype()
        {
        }

        protected bool Equals(Archetype other)
        {
            if (m_typeIds == null || other.m_typeIds == null)
            {
                return m_typeIds == null && other.m_typeIds == null;
            }

            return m_typeIds.SequenceEqual(other.m_typeIds);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Archetype)obj);
        }

        public override int GetHashCode()
        {
            return m_hashCodeCache;
        }

        internal class Database
        {
            /// <summary>
            /// Encodes the information of when a component is added/removed from
            /// an entity of a given archetype, what other archetype should it become? 
            /// </summary>
            private class JumpTable
            {
                // These are dictionaries that map from the action (adding/removing a certain
                // type of component), to the next archetype ID the entity should have
                public readonly Dictionary<int, short> addComponentTable = new();
                public readonly Dictionary<int, short> removeComponentTable = new();
            }

            // A list of known archetypes
            private readonly List<Archetype> m_archetypes = new();

            // Given a known archetype, find its ID
            private readonly Dictionary<Archetype, short> m_archetypeIds = new();

            // The jump tables for each known archetypes
            private readonly List<JumpTable> m_jumpTables = new();

            // The error archetype instance
            private readonly Archetype m_error = new();

            internal Database()
            {
                // The 0th archetype is always the error archetype
                AddOrGetArchetypeId(m_error);

                // The 1st archetype is the empty archetype
                AddOrGetArchetypeId(new Archetype(Enumerable.Empty<ComponentTypeId>()));
            }

            internal short Transition_AddComponent(short srcArchetypeId, ComponentTypeId componentToAdd)
            {
                var jumpTable = m_jumpTables[srcArchetypeId];
                if (jumpTable.addComponentTable.TryGetValue(componentToAdd, out var nextArchetypeId))
                {
                    return nextArchetypeId;
                }

                var srcArchetype = m_archetypes[srcArchetypeId];

                // If the component is already in the archetype, then return the error archetype
                // (since duplicates are not allowed)
                if (srcArchetype.m_typeIds.BinarySearch(componentToAdd) >= 0)
                {
                    return 0;
                }

                var nextArchetype = new Archetype(srcArchetype.m_typeIds.Append(componentToAdd));
                nextArchetypeId = AddOrGetArchetypeId(nextArchetype);
                jumpTable.addComponentTable.Add(componentToAdd, nextArchetypeId);
                return nextArchetypeId;
            }

            internal short Transition_RemoveComponent(short srcArchetypeId, ComponentTypeId componentToRemove)
            {
                var jumpTable = m_jumpTables[srcArchetypeId];
                if (jumpTable.removeComponentTable.TryGetValue(componentToRemove, out var nextArchetypeId))
                {
                    return nextArchetypeId;
                }

                var srcArchetype = m_archetypes[srcArchetypeId];

                // If the component is not in the archetype, then return the error archetype
                // (since you can't remove a component that doesn't exist)
                var componentIndex = srcArchetype.m_typeIds.BinarySearch(componentToRemove);
                if (componentIndex < 0)
                {
                    return 0;
                }

                var srcArchetypeTypes = srcArchetype.m_typeIds.ToList();
                srcArchetypeTypes.RemoveAt(componentIndex);
                var nextArchetype = new Archetype(srcArchetypeTypes);

                nextArchetypeId = AddOrGetArchetypeId(nextArchetype);
                jumpTable.removeComponentTable.Add(componentToRemove, nextArchetypeId);
                return nextArchetypeId;
            }

            internal Archetype GetById(short archetypeId)
            {
                return m_archetypes[archetypeId];
            }

            private short AddOrGetArchetypeId(Archetype archetype)
            {
                if (m_archetypeIds.TryGetValue(archetype, out var archetypeId))
                {
                    return archetypeId;
                }

                archetypeId = (short)m_archetypes.Count;
                m_archetypes.Add(archetype);
                m_archetypeIds.Add(archetype, archetypeId);
                m_jumpTables.Add(new JumpTable());
                return archetypeId;
            }
        }
    }
}