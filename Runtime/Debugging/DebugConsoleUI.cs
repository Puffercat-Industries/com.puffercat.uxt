using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Puffercat.Uxt.Debugging
{
    public class DebugConsoleUI : MonoBehaviour, DebugConsoleControl.IDebugConsoleActions
    {
        [SerializeField] private List<TMP_Text> _textboxes;
        [SerializeField] private TMP_InputField _inputArea;

        private List<string> _messagePrintBuffer;

        private DebugConsoleControl _consoleControl;

        private void Awake()
        {
            _messagePrintBuffer = new List<string>();

            _consoleControl = new DebugConsoleControl();
            _consoleControl.DebugConsole.Enable();
            _consoleControl.DebugConsole.SetCallbacks(this);

            foreach (var textBox in _textboxes)
            {
                textBox.gameObject.SetActive(false);
            }

            DebugConsole.onMessagePrinted += PrintMessage;

            gameObject.SetActive(false);
        }

        private void PrintMessage(string messageStr)
        {
            _messagePrintBuffer.Add(messageStr);
            while (_messagePrintBuffer.Count > _textboxes.Count)
            {
                _messagePrintBuffer.RemoveAt(0);
            }

            UpdateTextBoxes();
        }

        private void UpdateTextBoxes()
        {
            for (var i = 0; i < _messagePrintBuffer.Count; ++i)
            {
                _textboxes[i].gameObject.SetActive(true);
                _textboxes[i].text = _messagePrintBuffer[i];
            }

            for (var i = _messagePrintBuffer.Count; i < _textboxes.Count; ++i)
            {
                _textboxes[i].gameObject.SetActive(false);
                _textboxes[i].text = "";
            }
        }

        public void OnToggle(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(_inputArea.gameObject);
            }
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_inputArea.gameObject);
            if (context.performed &&
                gameObject.activeInHierarchy &&
                !string.IsNullOrEmpty(_inputArea.text.Trim()))
            {
                DebugConsole.SubmitCommand(_inputArea.text.Trim());
                _inputArea.text = "";
            }
        }
    }
}