using System;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public abstract class LifetimeExtendedScriptableObject : ScriptableObject, IExtendedLifetime
    {
        private bool m_alive;

        public void OnLifetimeEnded()
        {
            OnDisableOrLifetimeEnded();
        }

        public void OnDisable()
        {
            OnDisableOrLifetimeEnded();
        }
        
        private void OnDisableOrLifetimeEnded()
        {
            try
            {
                if (m_alive)
                {
                    OnLifeTimeEnded_Impl();
                }
            }
            finally
            {
                m_alive = false;
            }
        }

        public void OnEnable()
        {
            m_alive = false;
            OnEnable_Impl();
        }

        /// <summary>
        /// Call this function whenever a modification to the SO state happens
        /// and needs to be persisted across the play mode session.
        /// </summary>
        protected void ExtendLifetime()
        {
            if (m_alive) return;
            m_alive = true;
            ScriptableObjectLifetimeExtender.ExtendLifetime(this);
        }
        
        protected virtual void OnLifeTimeEnded_Impl()
        {
        }
        
        protected virtual void OnEnable_Impl()
        {
        }

    }
}