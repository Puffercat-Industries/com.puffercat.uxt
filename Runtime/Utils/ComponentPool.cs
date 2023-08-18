using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Puffercat.Uxt.Utils
{
    internal static class ComponentPoolStatics
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


    public class ComponentPool<T> : LifetimeExtendedScriptableObject where T : Component
    {
        [SerializeField] private T m_prefab;
        private Stack<T> m_pool;

        protected override void OnEnable_Impl()
        {
            base.OnEnable_Impl();
            m_pool = new Stack<T>();
        }

        protected override void OnLifeTimeEnded_Impl()
        {
            base.OnLifeTimeEnded_Impl();
            m_pool.Clear();
        }

        public T Instantiate(Transform parent = null, bool worldPositionStays = false)
        {
            ExtendLifetime();
            if (m_pool.TryPop(out var comp))
            {
                comp.transform.SetParent(parent, worldPositionStays);
                comp.gameObject.SetActive(true);
            }
            else
            {
                comp = Instantiate(m_prefab, parent, worldPositionStays);
            }

            var tr = comp.transform;
            tr.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            tr.localScale = Vector3.one;
            
            return comp;
        }

        public void Free(T comp)
        {
            ExtendLifetime();
            Debug.Assert(!m_pool.Contains(comp));
            comp.gameObject.SetActive(false);
            comp.transform.SetParent(ComponentPoolStatics.TransformRoot, false);
            m_pool.Push(comp);
        }

        public void FreeList(IList<T> compList)
        {
            if (compList is null) return;
            foreach (var comp in compList)
            {
                Free(comp);
            }

            compList.Clear();
        }
    }
}