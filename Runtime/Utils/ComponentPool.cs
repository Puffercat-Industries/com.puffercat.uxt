using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public static class ComponentPoolStatics
    {
        private static RectTransform s_rectTransformRoot;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StaticInit()
        {
            s_rectTransformRoot = null;
        }

        public static RectTransform TransformRoot
        {
            get
            {
                if (s_rectTransformRoot is null)
                {
                    var go = new GameObject("[Component Pool Root]");
                    s_rectTransformRoot = go.AddComponent<RectTransform>();
                    Object.DontDestroyOnLoad(s_rectTransformRoot);
                }

                return s_rectTransformRoot;
            }
        }
    }


    public class ComponentPool<T> : ScriptableObject where T : Component
    {
        [SerializeField] private T m_prefab;
    }
}