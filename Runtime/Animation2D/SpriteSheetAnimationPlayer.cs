using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Puffercat.Uxt.Animation2D
{
    public enum AnimationStatus
    {
        Stopped,
        Playing,
        Paused
    }

    [RequireComponent(typeof(Image))]
    public class SpriteSheetAnimationPlayer : MonoBehaviour
    {
        [SerializeField] private List<Sprite> m_sprites;
        [SerializeField] private float m_framesPerSecond;
        [SerializeField] private bool m_playOnAwake;
        [SerializeField] private bool m_hideOnFinished;
        [SerializeField] private bool m_loop;

        private Color m_color;

        private Image m_targetGraphics;

        private AnimationStatus m_status;
        private float m_playTime;
        private int m_currentFrame;

        private void Awake()
        {
            m_status = AnimationStatus.Stopped;
            m_targetGraphics = GetComponent<Image>();
            m_color = m_targetGraphics.color;
        }

        private void Start()
        {
            if (m_playOnAwake)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }

        private void LateUpdate()
        {
            if (m_status == AnimationStatus.Playing)
            {
                m_playTime += Time.deltaTime;
                m_currentFrame = (int)(m_playTime * m_framesPerSecond);
                if (m_currentFrame < m_sprites.Count)
                {
                    m_targetGraphics.sprite = m_sprites[m_currentFrame];
                }
                else
                {
                    if (m_loop)
                    {
                        m_playTime = 0;
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        public IEnumerator PlayAndWait()
        {
            Play();
            yield return new WaitUntil(() => m_status == AnimationStatus.Stopped);
        }

        public void Play()
        {
            m_status = AnimationStatus.Playing;
            m_targetGraphics.color = m_color;
        }

        public void Pause()
        {
            if (m_status == AnimationStatus.Playing)
            {
                m_status = AnimationStatus.Paused;
            }
        }

        public void Stop()
        {
            m_status = AnimationStatus.Stopped;
            m_playTime = 0.0f;
            m_currentFrame = 0;

            if (m_hideOnFinished)
            {
                m_targetGraphics.sprite = null;
                m_targetGraphics.color = Color.clear;
            }
        }
    }
}