using System;
using Object = UnityEngine.Object;

namespace Puffercat.Uxt.Utils
{
    public static class UnityObjectExtension
    {
#if UNITY_EDITOR
        /// <summary>
        /// Uses Unity's built-in serialization system to copy an object's field.
        /// Only use this in editor code.
        /// </summary>
        /// <param name="unityObject">The object to copy field from</param>
        /// <param name="fieldSelector">A function that selects in the object to copy. This only works if the field is
        /// serialized directly in <paramref name="unityObject"/> (i.e. not through a reference) </param>
        /// <returns></returns>
        public static TField CopyField<TObj, TField>(this TObj unityObject, Func<TObj, TField> fieldSelector)
            where TObj : Object
        {
            var tempCopy = Object.Instantiate(unityObject);
            var field = fieldSelector.Invoke(tempCopy);
            Object.DestroyImmediate(tempCopy);
            return field;
        }
#endif
    }
}