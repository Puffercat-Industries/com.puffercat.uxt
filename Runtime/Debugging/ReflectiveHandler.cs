using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Puffercat.Uxt.Debugging
{
    /// <summary>
    /// A handler that reacts to commands by looking at the method overloads
    /// of a class's member functions and determine the best function to call.
    /// </summary>
    public class ReflectiveHandler : ICommandHandler
    {
        private const BindingFlags MethodBindingFlags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private readonly object _instance;
        private readonly List<MethodInfo> _overloads;

        public ReflectiveHandler(object instance)
        {
            _instance = instance;
            _overloads = _instance
                .GetType()
                .GetMethods(MethodBindingFlags)
                .Where(m => m.GetCustomAttribute<DebugCommandAttribute>() != null)
                .ToList();
        }

        public void HandleCommand(string command)
        {
            var paramStrings = GetParameterStrings(command);
            var method = FindBestOverload(paramStrings, out var parsedParams);
            if (method == null)
            {
                DebugConsole.PrintMessage("No handler found for parameters");
                return;
            }

            method.Invoke(method.IsStatic ? null : _instance, parsedParams);
        }

        private static List<string> GetParameterStrings(string command)
        {
            var parameters = command.Split().ToList();
            Debug.Assert(parameters.Count >= 1);
            parameters.RemoveAt(0);
            return parameters;
        }

        private MethodInfo FindBestOverload(List<string> paramStrings, out object[] parsedParams)
        {
            foreach (var overload in _overloads)
            {
                if (OverloadMatchesParams(overload, paramStrings, out parsedParams))
                {
                    return overload;
                }
            }

            parsedParams = default;
            return null;
        }

        private static bool OverloadMatchesParams(
            MethodInfo overload,
            List<string> paramStrings,
            out object[] parsedParams)
        {
            parsedParams = default;
            var paramInfos = overload.GetParameters();
            if (paramStrings.Count != paramInfos.Length)
            {
                return false;
            }

            parsedParams = new object[paramInfos.Length];

            for (var i = 0; i < paramInfos.Length; ++i)
            {
                var paramInfo = paramInfos[i];
                var type = paramInfo.ParameterType;
                var paramStr = paramStrings[i];
                if (type == typeof(int) && int.TryParse(paramStr, out var intVal))
                {
                    parsedParams[i] = intVal;
                }
                else if (type == typeof(float) && float.TryParse(paramStr, out var floatVal))
                {
                    parsedParams[i] = floatVal;
                }
                else if (type.IsEnum && Enum.TryParse(type, paramStr, true, out var enumVal))
                {
                    parsedParams[i] = enumVal;
                }
                else if (type == typeof(string))
                {
                    parsedParams[i] = paramStr;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}