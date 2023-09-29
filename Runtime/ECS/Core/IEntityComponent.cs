namespace Puffercat.Uxt.ECS.Core
{
    public interface IEntityComponent<T> where T : struct, IEntityComponent<T>
    {
        public T Copy()
        {
            return (T)this;
        }
    }
}