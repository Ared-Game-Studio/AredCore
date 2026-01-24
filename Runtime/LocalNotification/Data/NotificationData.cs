using System;
using UnityEngine;
using NaughtyAttributes;

namespace Ared.Core.LocalNotification.Data
{
    [Serializable]
    public struct NotificationData
    {
        [Header("Content")]
        public string title;
        [ResizableTextArea] public string text;
        [BoxGroup("Timing")]
        public float time;
        [BoxGroup("Timing")]
        public ENotificationTimingMeasure timingMeasure;
    }
}