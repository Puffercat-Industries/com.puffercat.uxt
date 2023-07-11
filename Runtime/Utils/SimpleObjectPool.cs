using System.Collections.Generic;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    [ExecuteAlways]
    public class SimpleObjectPool<T> : MonoBehaviour where T : Component
    {
        [SerializeField] private T m_prefab;
        private List<T> m_pool;

        private void Awake()
        {
            m_pool = new List<T>();
        }

        public T Allocate(Transform parent = null, bool worldPositionStays = false)
        {
            T component;

            if (m_pool.Count == 0)
            {
                component = Instantiate(m_prefab, parent, worldPositionStays);
            }
            else
            {
                component = m_pool[^1];
                component.gameObject.SetActive(true);
                component.transform.SetParent(parent, worldPositionStays);
                m_pool.RemoveAt(m_pool.Count - 1);
            }

            if (!Application.IsPlaying(this))
            {
                component.gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }

            SetUpObject(component);

            return component;
        }

        public void Free(T component)
        {
            Debug.Assert(!m_pool.Contains(component), "!m_pool.Contains(component)");

            if (!Application.IsPlaying(this))
            {
                component.gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }

            component.gameObject.SetActive(false);
            component.transform.SetParent(transform, false);
            m_pool.Add(component);
        }

        public void FreeList(IList<T> components)
        {
            Debug.Assert(components != null, "components != null");
            foreach (var component in components)
            {
                Free(component);
            }
            components.Clear();
        }

        protected virtual void SetUpObject(T obj)
        {
        }

        protected virtual void TearDownObject(T obj)
        {
        }
    }
}