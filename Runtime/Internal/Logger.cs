using UnityEngine;

namespace Ared.Core.Internal
{
    public static class Logger
    {
        public enum LogOrigin
        {
            System,
            AutoSheetData,
            Analytics,
        }
        private static string GreenPrefix(LogOrigin origin) => $"<color=#00FF00>[{origin}]</color>";
        
        public static void Log(string message, LogOrigin origin) => Debug.Log($"{GreenPrefix(origin)} {message}");
        public static void LogWarning(string message, LogOrigin origin) => Debug.LogWarning($"{GreenPrefix(origin)} {message}");
        public static void LogError(string message, LogOrigin origin) => Debug.LogError($"{GreenPrefix(origin)} {message}");
    }
}