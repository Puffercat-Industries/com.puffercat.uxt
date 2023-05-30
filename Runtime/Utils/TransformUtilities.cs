using System.Collections.Generic;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public static class TransformUtilities
    {
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
    }
}