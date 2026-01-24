using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using Ared.Core.Internal;
using NaughtyAttributes;
using Ared.Core.LocalNotification.Data;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ared.Core.LocalNotification
{
    public class NotificationHandler : MonoBehaviour, ILogOrigin
    {
        [Header("Data")] 
        [SerializeField] private NotificationsConfig notificationsData;
        [Space, Header("Debugging")] 
        [InfoBox("If true, 'Time' in definitions is treated as Seconds.")] [SerializeField]
        private bool isTestMode;


        public ILogOrigin Logger => this;
        public ELogOrigin LogOrigin => ELogOrigin.LocalNotification;
        


        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (notificationsData == null)
            {
                Logger.LogError("Notifications Config is not assigned.");
                return;
            }
            
            InitializeAndroid();
            InitializeIOS();

            CancelAllNotifications();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                CancelAllNotifications();
            }
            else
            {
                ScheduleAllNotifications();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                ScheduleAllNotifications();
            }
            else
            {
                CancelAllNotifications();
            }
        }

        private void OnApplicationQuit()
        {
            ScheduleAllNotifications();
        }

        //*************************** Android ***************************//

        private void InitializeAndroid()
        {
#if UNITY_ANDROID
            var channel = new AndroidNotificationChannel()
            {
                Id = notificationsData.AndroidChannelId,
                Name = notificationsData.AndroidChannelName,
                Importance = Importance.Default,
                Description = notificationsData.AndroidChannelDescription,
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);

            // Request permission for Android 13+
            StartCoroutine(RequestNotificationPermission());
#endif
        }

        private IEnumerator RequestNotificationPermission()
        {
#if UNITY_ANDROID
            var request = new PermissionRequest();
            if (request.Status == PermissionStatus.RequestPending)
                yield return request;
            Debug.Log("Permission result: " + request.Status);
#else
        yield return null;
#endif
        }

        //***************************** iOS *****************************//

        private void InitializeIOS()
        {
#if UNITY_IOS
        StartCoroutine(RequestAuthorization());
#endif
        }
        
        private IEnumerator RequestAuthorization()
        {
#if UNITY_IOS
            var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
            using (var req = new AuthorizationRequest(authorizationOption, true))
            {
                while (!req.IsFinished)
                {
                    yield return null;
                };

                string res = "\n RequestAuthorization:";
                res += "\n finished: " + req.IsFinished;
                res += "\n granted :  " + req.Granted;
                res += "\n error:  " + req.Error;
                res += "\n deviceToken:  " + req.DeviceToken;
                Debug.Log(res);
            }
#else
            yield return null;
#endif
        }

        //************************** Scheduling **************************//

        private void ScheduleAllNotifications()
        {
#if UNITY_EDITOR
            return;
#endif
            
            if (notificationsData == null) return;
            
            if (!notificationsData.EnableNotifications) return;

            CancelAllNotifications();

            var filteredNotifications = notificationsData.Notifications
                .GroupBy(n => new { n.time, n.timingMeasure })
                .Select(g => g.ElementAt(UnityEngine.Random.Range(0, g.Count())))
                .ToList();

            foreach (NotificationData notifData in filteredNotifications)
            {
                ScheduleNotification(notifData, notificationsData);
            }

            Logger.Log($"[NotificationHandler] Scheduled {filteredNotifications.Count} notifications (Test Mode: {isTestMode})");
        }

        private void ScheduleNotification(NotificationData notifData, NotificationsConfig allData)
        {
            // Calculate time
            DateTime fireTime = notifData.timingMeasure switch
            {
                ENotificationTimingMeasure.Seconds => DateTime.Now.AddSeconds(notifData.time),
                ENotificationTimingMeasure.Minutes => DateTime.Now.AddMinutes(notifData.time),
                ENotificationTimingMeasure.Hours => DateTime.Now.AddHours(notifData.time),
                _ => DateTime.Now.AddMinutes(notifData.time)
            };

            if (isTestMode)
            {
                fireTime = DateTime.Now.AddSeconds(notifData.time);
            }

#if UNITY_ANDROID
            var notification = new AndroidNotification();

            notification.Title = notifData.title;
            notification.Text = notifData.text;
            notification.FireTime = fireTime;
            if (!string.IsNullOrEmpty(allData.SmallIconID))
                notification.SmallIcon = allData.SmallIconID;
            if (!string.IsNullOrEmpty(allData.LargeIconID))
                notification.LargeIcon = allData.LargeIconID;

            var id = AndroidNotificationCenter.SendNotification(notification, allData.AndroidChannelId);
            Logger.Log($"[NotificationHandler] Scheduled Android notification ID: {id} - FireTime: {fireTime}");
#endif

#if UNITY_IOS
            TimeSpan timeInterval = notifData.timingMeasure switch
            {
                ENotificationTimingMeasure.Seconds => TimeSpan.FromSeconds(notifData.time),
                ENotificationTimingMeasure.Minutes => TimeSpan.FromMinutes(notifData.time),
                ENotificationTimingMeasure.Hours => TimeSpan.FromHours(notifData.time),
            };

            if (isTestMode)
            {
                timeInterval = TimeSpan.FromSeconds(notifData.time);
            }

            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = timeInterval,
                Repeats = false
            };

            var iosNotification = new iOSNotification()
            {
                Identifier = Guid.NewGuid().ToString(),
                Title = notifData.title,
                Body = notifData.text,
                ShowInForeground = true,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                CategoryIdentifier = "category_a",
                ThreadIdentifier = "thread1",
                Trigger = timeTrigger,
            };

            iOSNotificationCenter.ScheduleNotification(iosNotification);
#endif
        }

        private void CancelAllNotifications()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllNotifications();
#endif
#if UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }
        
        
        
        
#if UNITY_EDITOR
        [MenuItem("Ared/LocalNotification/Create Notification Handler")]
        private static void CreateNotificationHandler()
        {
            GameObject go = new GameObject("_NotificationHandler_");
            var handler = go.AddComponent<NotificationHandler>();
            
            //set config asset if exists
            NotificationsConfig config = AssetDatabase.LoadAssetAtPath<NotificationsConfig>(NotificationsConfig.ConfigDefaultPath);
            if (config != null)
            {
                handler.notificationsData = config;
                EditorUtility.SetDirty(go);
            }
            
            Undo.RegisterCreatedObjectUndo(go, "Create Notification Handler");
            Selection.activeObject = go;
        }
#endif
    }
}