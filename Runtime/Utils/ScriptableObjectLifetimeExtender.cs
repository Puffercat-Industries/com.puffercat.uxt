using System.Collections.Generic;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public class ScriptableObjectLifetimeExtender : MonoBehaviour
    {
        private HashSet<IExtendedLifetime> m_keepAlive;

        private static ScriptableObjectLifetimeExtender s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StaticInit()
        {
            s_instance = null;
        }

        private static ScriptableObjectLifetimeExtender Instance
        {
            get
            {
                if (s_instance is null)
                {
                    var go = new GameObject("[Scriptable Object Lifetime Extender]");
                    s_instance = go.AddComponent<ScriptableObjectLifetimeExtender>();
                    DontDestroyOnLoad(go);
                }

                return s_instance;
            }
        }

        public static void ExtendLifetime(IExtendedLifetime obj)
        {
            Instance.m_keepAlive.Add(obj);
        }

        private void Awake()
        {
            m_keepAlive = new HashSet<IExtendedLifetime>();
        }

        private void OnDestroy()
        {
            foreach (var extendedLifetime in m_keepAlive)
            {
                extendedLifetime.OnLifetimeEnded();
            }
            m_keepAlive.Clear();
        }
    }

    public interface IExtendedLifetime
    {
        void OnLifetimeEnded();
    }
}