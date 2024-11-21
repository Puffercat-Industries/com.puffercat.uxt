using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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

        public static T InstantiateAsPrefabIfPossible<T>(this T obj, Transform parent) where T : Object
        {
            if (obj.GetTransform() == null)
            {
                throw new ArgumentException("Object must be a GameObject or a Component");
            }

#if UNITY_EDITOR
            if ((!parent && !Application.isPlaying || !Application.IsPlaying(parent)) &&
                PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                var instance = (T)PrefabUtility.InstantiatePrefab(obj, parent);
                return instance;
            }
#endif
            return Object.Instantiate(obj, parent);
        }

        private static Transform GetTransform(this Object obj)
        {
            if (obj is Component component)
            {
                return component.transform;
            }

            if (obj is GameObject gameObject)
            {
                return gameObject.transform;
            }

            return null;
        }
    }
}