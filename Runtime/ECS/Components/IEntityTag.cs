using Puffercat.Uxt.ECS.Core;

namespace Puffercat.Uxt.ECS.Components
{
    public interface IEntityTag<T>: IEntityComponent<T> where T : struct, IEntityTag<T>
    {
        T IEntityComponent<T>.Copy()
        {
            return default;
        }
    }
}