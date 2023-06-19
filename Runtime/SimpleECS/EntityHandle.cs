namespace Puffercat.Uxt.SimpleECS
{
    public struct EntityHandle
    {
        internal bool isValid;
        internal int persistentId;
        internal ulong version;
        
        /// <summary>
        /// Get the entity that this entity handle references
        /// in the entity registry.
        /// </summary>
        /// <param name="registry">The registry that this entity handle was obtained from</param>
        /// <returns></returns>
        public Entity Get(EntityRegistry registry)
        {
            return registry.Get(this);
        }
    }
}