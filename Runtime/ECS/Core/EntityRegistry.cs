namespace Puffercat.Uxt.ECS.Core
{
    public class EntityRegistry
    {
        public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
        {
            throw new System.NotImplementedException();
        }

        public ref T AddComponent<T>(Entity entity) where T : struct, IComponent
        {
            throw new System.NotImplementedException();
        }
    }

    public class ComponentRegistry
    {
        private IntSparseMap<EntityComponentLink> m_e2cLinks;
    }
}