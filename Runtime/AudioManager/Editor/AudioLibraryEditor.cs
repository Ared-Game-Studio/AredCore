using UnityEditor;
using UnityEngine;
using Ared.Core.AudioManager.Data;

namespace Ared.Core.AudioManager.Editor
{
    [CustomEditor(typeof(AudioLibrary))]
    public class AudioLibraryEditor : UnityEditor.Editor
    {
        private AudioLibrary _audioLibrary;

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Audio Library"))
            {
                AudioLibraryWindow.Open();
            }
        }
    }
}