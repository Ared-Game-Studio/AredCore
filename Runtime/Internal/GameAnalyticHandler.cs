using System;
using System.Collections.Generic;
using UnityEngine;
using GameAnalyticsSDK;
using Logger = Ared.Core.Internal.Logger;

namespace Ared.Core.Internal
{
    public class GameAnalyticHandler : IGameAnalyticsATTListener
    {
        private static bool _isInitialized = false;
        
        public GameAnalyticHandler()
        {
#if UNITY_EDITOR
            LogAnalyticsEvent("System", "Initialize", "Editor mode - events will be logged to console");
            _isInitialized = true;
#else
            InitializeSdk();
#endif
        }
        
        private void InitializeSdk()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                GameAnalytics.RequestTrackingAuthorization(this);
            }
            else
            {
                GameAnalytics.Initialize();
            }

            _isInitialized = true;
        }
        
        // Event Wrappers --------------------------------------------------------------------------------------
        
        public static void CustomEvent(string eventName)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Custom", eventName);
#else
            GameAnalytics.NewDesignEvent(eventName);
#endif
        }

        public static void CustomEvent(string eventName, float value)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Custom", eventName, $"Value: {value}");
#else
            GameAnalytics.NewDesignEvent(eventName, value);
#endif
        }

        public static void CustomEvent(string eventName, params (string paramName, IConvertible param)[] items)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            string parameters = string.Join(", ", Array.ConvertAll(items, item => $"{item.paramName}: {item.param}"));
            LogAnalyticsEvent("Custom", eventName, $"Parameters: {parameters}");
#else
            Dictionary<string, object> eventParams = new Dictionary<string, object>();
            foreach ((string paramName, IConvertible param) item in items)
            {
                eventParams.Add(item.paramName, item.param);
            }
            GameAnalytics.NewDesignEvent(eventName, eventParams);
#endif
        }

        public static void StartTutorial(string tutorialKey, string LevelId, int levelIndex)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Tutorial", $"Start: {tutorialKey}", $"LevelId: {LevelId}, Index: {levelIndex}");
#else
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, tutorialKey, levelIndex);
#endif
        }

        public static void CompleteTutorial(string tutorialKey, string LevelId, int levelIndex)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Tutorial", $"Complete: {tutorialKey}", $"LevelId: {LevelId}, Index: {levelIndex}");
#else
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, tutorialKey, levelIndex);
#endif
        }

        public static void StartLevel(string levelId, int levelIndex, int? score)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Level", $"Start: {levelId}", $"Index: {levelIndex}, Score: {score}");
#else
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, levelId, levelIndex.ToString());
#endif
        }

        public static void LevelCompleted(string levelId, int levelIndex, int? score)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Level", $"Complete: {levelId}", $"Index: {levelIndex}, Score: {score}");
#else
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, levelId, levelIndex.ToString());
#endif
        }

        public static void LevelFailed(string levelId, int levelIndex, int? score)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Level", $"Failed: {levelId}", $"Index: {levelIndex}, Score: {score}");
#else
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, levelId, levelIndex.ToString());
#endif
        }

        public static void EarnCurrency(string currencyType, int count, string earnType, string itemId)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Resource", $"Earn: {currencyType}", $"Count: {count}, EarnType: {earnType}, ItemId: {itemId}");
#else
            GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, currencyType, count, earnType, itemId);
#endif
        }

        public static void SpendCurrency(string currencyType, int count, string spendType, string itemId)
        {
            if (!_isInitialized) return;
#if UNITY_EDITOR
            LogAnalyticsEvent("Resource", $"Spend: {currencyType}", $"Count: {count}, SpendType: {spendType}, ItemId: {itemId}");
#else
            GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, currencyType, count, spendType, itemId);
#endif
        }
        
        // Callbacks --------------------------------------------------------------------------------------------
        
        public void GameAnalyticsATTListenerNotDetermined()
        {
            GameAnalytics.Initialize();
        }

        public void GameAnalyticsATTListenerRestricted()
        {
            GameAnalytics.Initialize();
        }

        public void GameAnalyticsATTListenerDenied()
        {
            GameAnalytics.Initialize();
        }

        public void GameAnalyticsATTListenerAuthorized()
        {
            GameAnalytics.Initialize();
        }
        
        // Log Wrappers -----------------------------------------------------------------------------------------
#if UNITY_EDITOR
        // ReSharper disable Unity.PerformanceAnalysis
        private static void LogAnalyticsEvent(string eventType, string eventName, string details = null)
        {
            string colorHex = "#9b59b6"; // Purple

            string message = $"<color={colorHex}>[GameAnalytics]</color> <b>{eventType}</b>: {eventName}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }

            Logger.Log(message, Logger.LogOrigin.Analytics);
        }
#endif
    }
}