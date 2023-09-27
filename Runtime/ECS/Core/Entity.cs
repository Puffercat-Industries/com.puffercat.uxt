namespace Puffercat.Uxt.ECS.Core
{
    /// <summary>
    /// An Entity is an object that can store a list of components.
    /// Every entity residing in a registry at a given time has a unique ID.
    /// When an entity is destroyed, its ID can be reused. Therefore, each handle
    /// to an entity has a version associated with it.
    /// </summary>
    public readonly struct Entity
    {
        public readonly int id;
        public readonly ulong version;

        internal Entity(int id, ulong version)
        {
            this.id = id;
            this.version = version;
        }
    }
    
    public readonly struct EntityComponentLink
    {
        public readonly int componentAddress;
        
        internal EntityComponentLink(int componentAddress)
        {
            this.componentAddress = componentAddress;
        }
    }
}