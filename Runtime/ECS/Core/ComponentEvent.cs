namespace Puffercat.Uxt.ECS.Core
{
    public enum ComponentEventType
    {
        CreationOrModification,
        Destruction
    }

    public struct ComponentEvent
    {
        public readonly Entity entity;
        public readonly ComponentTypeId componentTypeId;
        public readonly ComponentEventType eventType;
        public readonly int index;

        internal ComponentEvent(Entity entity, ComponentTypeId componentTypeId, ComponentEventType eventType, int index)
        {
            this.entity = entity;
            this.componentTypeId = componentTypeId;
            this.eventType = eventType;
            this.index = index;
        }
    }
}