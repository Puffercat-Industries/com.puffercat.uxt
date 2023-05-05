using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Puffercat.Uxt.Debugging
{
    public static class DebugConsole
    {
        public static event Action<string> onMessagePrinted;
        private static Dictionary<string, ICommandHandler> _commandHandlers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void StaticInit()
        {
            onMessagePrinted = null;
            _commandHandlers = new Dictionary<string, ICommandHandler>();
        }

        public static void SubmitCommand(string command)
        {
            var prefix = (command.Split().FirstOrDefault() ?? "").Trim().ToLower();
            if (!_commandHandlers.TryGetValue(prefix, out var handler))
            {
                PrintMessage($"Command '{prefix}' is not found.");
            }
            else
            {
                handler.HandleCommand(command);
            }
        }

        public static void RegisterHandler(string prefix, ICommandHandler handler)
        {
            prefix = prefix.Trim();
            Debug.Assert(prefix.All(c => char.IsDigit(c) || char.IsLetter(c) || c == '_'));

            if (_commandHandlers.TryGetValue(prefix, out var oldHandler))
            {
                Debug.LogError($"'{prefix}' is already registered as a command handled by '{oldHandler}'");
                return;
            }

            _commandHandlers.Add(prefix, handler);
        }

        public static void UnregisterHandler(string prefix)
        {
            if (!_commandHandlers.Remove(prefix))
            {
                Debug.LogError($"'{prefix}' is not registered as a command.");
            }
        }

        public static ReflectiveHandler CreateHandlerFromObject(object obj)
        {
            return new ReflectiveHandler(obj);
        }

        public static void PrintMessage(string message)
        {
            onMessagePrinted?.Invoke(message);
        }
    }
}