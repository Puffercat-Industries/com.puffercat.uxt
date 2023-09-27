using System.Runtime.InteropServices;

namespace Puffercat.Uxt.Utils
{
    public static class UnsafeUtils
    {
        public static ref T BlessReference<T>(ref T reference)
        {
            return ref MemoryMarshal.CreateSpan(ref reference, 1)[0];
        }
    }
}