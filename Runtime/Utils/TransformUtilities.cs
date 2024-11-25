using System.Collections.Generic;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public static class TransformUtilities
    {
        public static void DestroyAllChildrenImmediate(this Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; --i)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }
        
        public static void DestroyAllChildren(this Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; --i)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }
        
        public static void GetRootComponents<T>(this Transform root, List<T> outputList)
        {
            if (root.TryGetComponent(out T comp))
            {
                outputList.Add(comp);
            }
            else
            {
                GetRootComponentsInChildren(root, outputList);
            }
        }

        public static void GetRootComponentsInChildren<T>(this Transform root, List<T> outputList)
        {
            for (var i = 0; i < root.childCount; ++i)
            {
                GetRootComponents(root.GetChild(i), outputList);
            }
        }

        public static void UnParentAllChildren(this Transform root, bool destroySelf = true)
        {
            for (var i = root.childCount; i >= 0; --i)
            {
                root.GetChild(i).SetParent(root.parent);
            }

            if (destroySelf)
            {
                Object.Destroy(root.gameObject);
            }
        }

        public static Camera GetCanvasWorldCamera(this RectTransform rectTransform)
        {
            while (true)
            {
                if (rectTransform.parent is RectTransform rectTransformParent)
                {
                    rectTransform = rectTransformParent;
                }
                else
                {
                    var canvas = rectTransform.GetComponent<Canvas>();

                    Debug.Assert(
                        canvas.renderMode != RenderMode.WorldSpace, 
                        "Querying the camera of a world space canvas does not make sense.");
                    
                    if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        return canvas.worldCamera;
                    }

                    return null;
                }
            }
        }

        public static void SetIdentityTransform(this Transform transform)
        {
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }

        public static T WithIdentityTransform<T>(this T component) where T : Component
        {
            component.transform.SetIdentityTransform();
            return component;
        }

        public static GameObject WithIdentityTransform(this GameObject go)
        {
            go.transform.SetIdentityTransform();
            return go;
        }

        public static GameObject WithParent(this GameObject go, Transform parent)
        {
            go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject WithHideFlags(this GameObject go, HideFlags flags)
        {
            go.hideFlags = flags;
            return go;
        }
    }
}