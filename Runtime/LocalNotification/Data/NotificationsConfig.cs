using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ared.Core.LocalNotification.Data
{
    [CreateAssetMenu(fileName = "NotificationsConfig", menuName = "LocalNotification/NotificationsConfig")]
    public class NotificationsConfig : ScriptableObject
    {
        [field:SerializeField] public bool EnableNotifications { get; private set; } = true;
        [field: SerializeField] public NotificationData[] Notifications { get; private set; }
        [field:HorizontalLine, SerializeField] public string SmallIconID { get; private set; }
        [field:SerializeField] public string LargeIconID { get; private set; }
        [field:Header("Settings"), SerializeField] public string AndroidChannelId { get; private set; } = "game_notifications";
        [field:SerializeField] public string AndroidChannelName { get; private set; } = "Game Notifications";
        [field:SerializeField] public string AndroidChannelDescription { get; private set; } = "Reminders to come back to the game";
        
        
        
#if UNITY_EDITOR
        private const string ConfigFolder = "Assets/AssetData/Notifications/";
        private const string ConfigFileName = "NotificationsConfig.asset";
        public const string ConfigDefaultPath = ConfigFolder + ConfigFileName;
        [MenuItem("Ared/LocalNotification/Create Notifications Config")]
        private static void CreateConfigAsset()
        {
            NotificationsConfig config = AssetDatabase.LoadAssetAtPath<NotificationsConfig>(ConfigDefaultPath);
            if (config == null)
            {
                if (!System.IO.Directory.Exists(ConfigFolder))
                {
                    System.IO.Directory.CreateDirectory(ConfigFolder);
                    AssetDatabase.Refresh();
                }

                config = CreateInstance<NotificationsConfig>();
                AssetDatabase.CreateAsset(config, ConfigDefaultPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
#endif
    }
}