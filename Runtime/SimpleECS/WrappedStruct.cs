namespace Puffercat.Uxt.SimpleECS
{
    public class WrappedStruct<T> : IComponent where T : struct
    {
        private T m_data;

        public ref T Data => ref m_data;

        public IComponent Copy()
        {
            return new WrappedStruct<T>
            {
                m_data = m_data
            };
        }
    }
}