using System.Collections.Generic;

namespace Puffercat.Uxt.Utils
{
    public static class EnumerableExt
    {
        public static void ToList<T>(this IEnumerable<T> e, List<T> outputList)
        {
            outputList.Clear();
            outputList.AddRange(e);
        }
    }
}