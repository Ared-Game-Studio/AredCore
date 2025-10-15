using UnityEditor;
using UnityEngine;
using System.IO;
using Ared.AutoSheetData.Data;

namespace Ared.AutoSheetData.Editor
{
    public class SpreadsheetConfigWindow : EditorWindow
    {
        private SpreadsheetConfig _config;
        private UnityEditor.Editor _configEditor;

        [MenuItem("Ared/AutoSheetData/Open Config")]
        public static void Open()
        {
            var window = GetWindow<SpreadsheetConfigWindow>("AutoSheetData Config");
            window.Show();
        }

        private void OnEnable()
        {
            EnsureConfigExists();
            BuildEditor();
        }

        private void OnFocus()
        {
            if (_config == null) EnsureConfigExists();
            BuildEditor();
        }

        private void OnDestroy()
        {
            if (_configEditor != null)
            {
                DestroyImmediate(_configEditor);
                _configEditor = null;
            }
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox("No SpreadsheetConfig found and failed to create one.", MessageType.Error);
                if (GUILayout.Button("Retry Create Config"))
                {
                    EnsureConfigExists();
                    BuildEditor();
                }
                return;
            }

            if (_configEditor == null) BuildEditor();
            
            if (_configEditor != null)
            {
                _configEditor.OnInspectorGUI();
            }
        }

        private void EnsureConfigExists()
        {
            // Try find existing
            var guids = AssetDatabase.FindAssets("t:AutoSheetData.Data.SpreadsheetConfig");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<SpreadsheetConfig>(path);
                return;
            }

            // Create new one
            const string root = "Assets/AutoAssetData";
            if (!AssetDatabase.IsValidFolder(root))
            {
                AssetDatabase.CreateFolder("Assets", "AutoAssetData");
            }

            var assetPath = $"{root}/SpreadsheetConfig.asset";
            var instance = CreateInstance<SpreadsheetConfig>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _config = instance;
            Debug.Log($"<color=#00FF00>[AutoSheetData]</color> Created config at: {assetPath}");
        }

        private void BuildEditor()
        {
            if (_config == null) return;
            if (_configEditor != null)
            {
                // Rebuild if target changed
                if (_configEditor.target != _config)
                {
                    DestroyImmediate(_configEditor);
                    _configEditor = UnityEditor.Editor.CreateEditor(_config);
                }
            }
            else
            {
                _configEditor = UnityEditor.Editor.CreateEditor(_config);
            }
        }
    }
}