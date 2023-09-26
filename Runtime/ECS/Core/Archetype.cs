using System;
using System.Collections.Generic;
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
        private readonly List<TypeId> m_typeIds;
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

        public Archetype(IEnumerable<TypeId> componentTypes)
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

        #region Static archetype database

        // A list of known archetypes
        private static readonly List<Archetype> s_archetypes = new();

        // Given a known archetype, find its ID
        private static Dictionary<Archetype, short> s_archetypeIds = new();
        
        
        
        #endregion
    }
}