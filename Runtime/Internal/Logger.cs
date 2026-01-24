using UnityEngine;

namespace Ared.Core.Internal
{
    public static class Logger
    {
        private static string GreenPrefix(ELogOrigin origin) => $"<color=#00FF00>[{origin}]</color>";
        
        public static void Log(string message, ELogOrigin origin) => Debug.Log($"{GreenPrefix(origin)} {message}");
        public static void LogWarning(string message, ELogOrigin origin) => Debug.LogWarning($"{GreenPrefix(origin)} {message}");
        public static void LogError(string message, ELogOrigin origin) => Debug.LogError($"{GreenPrefix(origin)} {message}");
    }
}