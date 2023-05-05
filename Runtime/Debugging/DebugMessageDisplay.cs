using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Puffercat.Uxt.Debugging
{
    public class DebugMessageDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _textBoxPrefab;
        [SerializeField] private RectTransform _textBoxRoot;
        private List<TMP_Text> _textBoxes;

        private void Awake()
        {
            _textBoxes = new List<TMP_Text>();
        }

        private void Update()
        {
            var messages = DebugMessageManager.Messages;

            if (_textBoxes.Count > messages.Count)
            {
                for (var i = messages.Count; i < _textBoxes.Count; ++i)
                {
                    Destroy(_textBoxes[i].gameObject);
                }

                _textBoxes.RemoveRange(messages.Count, _textBoxes.Count - messages.Count);
            }

            while (_textBoxes.Count < messages.Count)
            {
                _textBoxes.Add(Instantiate(_textBoxPrefab, _textBoxRoot));
            }

            Debug.Assert(_textBoxes.Count == messages.Count);
            for (var i = 0; i < messages.Count; ++i)
            {
                var textBox = _textBoxes[i];
                var message = messages[i];

                textBox.color = message.Color;
                textBox.text = message.Text;
            }

            DebugMessageManager.Tick(Time.deltaTime);
        }
    }
}