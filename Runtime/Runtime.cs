using JetBrains.Annotations;
using UnityEngine;

namespace Puffercat.Uxt
{
    public static class Runtime
    {
        /// <summary>
        /// Similar to <see cref="Debug.Assert(bool)"/>, but
        /// would always evaluate the parameter <paramref name="val"/>
        /// </summary>
        /// <param name="val">The value to be asserted</param>
        /// <param name="message">The assertion message</param>
        [ContractAnnotation("val:false=>halt")]
        public static void Assert(bool val, string message = "Assertion failed.")
        {
            Debug.Assert(val, message);
        }
    }
}