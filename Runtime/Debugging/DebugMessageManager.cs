using System.Collections.Generic;
using UnityEngine;

namespace Puffercat.Uxt.Debugging
{
    public static class DebugMessageManager
    {
        public class Message
        {
            public int Id { get; set; }
            public float TimeRemaining { get; set; }
            public string Text { get; set; }
            public Color Color { get; set; }
        }

        private static int _nextId;
        private static Dictionary<int, Message> _indexedMessages;
        private static List<Message> _messages;
        private static List<Message> _expiredMessageBuffer;

        public static IReadOnlyList<Message> Messages => _messages;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StaticReset()
        {
            _nextId = 0;
            _indexedMessages = new Dictionary<int, Message>();
            _messages = new List<Message>();
            _expiredMessageBuffer = new List<Message>();
        }

        public static int AllocateId() => _nextId++;

        public static void AddOnScreen(string text) => AddOnScreen(text, -1, Color.red);

        public static void AddOnScreen(string text, int id, Color color, float duration = 1.5f)
        {
            if (id == -1)
            {
                var message = new Message()
                {
                    TimeRemaining = duration,
                    Text = text,
                    Color = color,
                    Id = -1
                };
                _messages.Add(message);
            }
            else
            {
                if (_indexedMessages.TryGetValue(id, out var message))
                {
                    message.TimeRemaining = duration;
                    message.Text = text;
                    message.Color = color;
                }
                else
                {
                    message = new Message
                    {
                        TimeRemaining = duration,
                        Text = text,
                        Color = color,
                        Id = id
                    };
                    _indexedMessages.Add(id, message);
                    _messages.Add(message);
                }
            }
        }

        public static void Tick(float deltaTime)
        {
            _expiredMessageBuffer.Clear();
            foreach (var message in _messages)
            {
                message.TimeRemaining -= deltaTime;
                if (message.TimeRemaining < 0.0f)
                {
                    _expiredMessageBuffer.Add(message);
                }
            }
            _messages.RemoveAll(m => m.TimeRemaining < 0.0f);
            foreach (var expired in _expiredMessageBuffer)
            {
                _indexedMessages.Remove(expired.Id);
            }
        }
    }
}