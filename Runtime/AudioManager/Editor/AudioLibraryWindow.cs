#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Ared.Core.AudioManager.Data;
using Ared.Core.Internal;

namespace Ared.Core.AudioManager.Editor
{
    public class AudioLibraryWindow : EditorWindow, ILogOrigin
    {
        private const string LibraryAssetPath = "Assets/Audio/AudioLibrary.asset";
        private const string GeneratedFolderPath = "Assets/Audio/Generated";
        private const string GeneratedPath = "Assets/Audio/Generated/AudioEnums.cs"; 
        
        private AudioLibrary _library;
        private SerializedObject _serializedLibrary;
        
        private Vector2 _scroll;
        
        [MenuItem("Ared/AudioManager/Open Audio Library")]
        public static void Open()
        {
            var window = GetWindow<AudioLibraryWindow>("Audio Library");
            window.minSize = new Vector2(500, 600);
            window.EnsureLibraryExists();
        }
        
        private void OnEnable()
        {
            EnsureLibraryExists();
        }

        private void EnsureLibraryExists()
        {
            _library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryAssetPath);
            
            if (_library == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryAssetPath) ?? "Assets/Audio");
                _library = CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(_library, LibraryAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Logger.Log($"Created new AudioLibrary at {LibraryAssetPath}");
            }

            _serializedLibrary = new SerializedObject(_library);
        }

        private void OnGUI()
        {
            if (_library == null)
            {
                EnsureLibraryExists();
            }

            _serializedLibrary.Update();

            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSection("Music", _serializedLibrary.FindProperty("musics"));
            EditorGUILayout.Space(10);
            DrawSection("SFX", _serializedLibrary.FindProperty("sfxs"));

            EditorGUILayout.EndScrollView();

            _serializedLibrary.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            DrawFooter();
        }

        private void DrawSection(string label, SerializedProperty listProp)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            // Drag and drop area
            DrawDragAndDropArea(listProp, label);

            // List elements
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty element = listProp.GetArrayElementAtIndex(i);
                SerializedProperty clipId = element.FindPropertyRelative("id");
                SerializedProperty clipProp = element.FindPropertyRelative("clip");
                SerializedProperty volumeProp = element.FindPropertyRelative("volume");

                EditorGUILayout.BeginHorizontal();
                
                // Name/ID field
                clipId.stringValue = EditorGUILayout.TextField(clipId.stringValue, GUILayout.Width(150));
                clipId.stringValue = SanitizeName(clipId.stringValue);

                // Clip field
                EditorGUILayout.PropertyField(clipProp, GUIContent.none, GUILayout.MinWidth(150));
                
                // Volume slider
                EditorGUILayout.LabelField("Vol", GUILayout.Width(30));
                volumeProp.floatValue = EditorGUILayout.Slider(volumeProp.floatValue, 0f, 1f);

                // Remove button
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Add New {label}"))
            {
                listProp.arraySize++;
                var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                newElement.FindPropertyRelative("volume").floatValue = 1f;
                newElement.FindPropertyRelative("id").stringValue = "New" + label;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDragAndDropArea(SerializedProperty listProp, string label)
        {
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, $"Drag {label} Clips Here");

            var evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        var clip = obj as AudioClip;
                        if (clip == null) continue;

                        listProp.arraySize++;
                        var element = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                        element.FindPropertyRelative("clip").objectReferenceValue = clip;
                        element.FindPropertyRelative("volume").floatValue = 1f;
                        element.FindPropertyRelative("id").stringValue = SanitizeName(clip.name);
                    }
                }
                evt.Use();
            }
        }

        private void DrawFooter()
        {
            bool hasDuplicate = _library.HasDuplicateNames(out string duplicateName);
            bool hasEmpty = _library.HasEmptyNames();
            bool isDirty = _library.lastGeneratedHash != _library.ComputeHash();

            if (hasDuplicate)
            {
                EditorGUILayout.HelpBox($"Duplicate audio name found: {duplicateName}", MessageType.Error);
            }
            
            if (hasEmpty)
            {
                EditorGUILayout.HelpBox("One or more audio entries have empty names.", MessageType.Error);
            }

            Color oldColor = GUI.color;
            if (isDirty) GUI.color = new Color(1f, 0.85f, 0.2f);

            EditorGUI.BeginDisabledGroup(hasDuplicate || hasEmpty);

            if (GUILayout.Button("Generate Enums", GUILayout.Height(32)))
            {
                GenerateCode();
                _library.lastGeneratedHash = _library.ComputeHash();
                EditorUtility.SetDirty(_library);
                AssetDatabase.SaveAssets();
            }

            EditorGUI.EndDisabledGroup();
            GUI.color = oldColor;
        }
        
        private void GenerateCode()
        {
            Directory.CreateDirectory(GeneratedFolderPath);
            
            var musicNames = _library.musics
                .Where(e => e.clip is not null)
                .Select(e => e.id)
                .ToArray();

            var sfxNames = _library.sfxs
                .Where(e => e.clip is not null)
                .Select(e => e.id)
                .ToArray();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Auto-generated by AudioManager");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Ared.Core.AudioManager;");
            sb.AppendLine();
            
            // 1. Generate Enums
            AppendEnum(sb, "EMusicEnums", musicNames);
            AppendEnum(sb, "ESfxEnums", sfxNames);

            // 2. Generate Static API Wrapper
            sb.AppendLine("public static class AudioPlayer");
            sb.AppendLine("{");
            
            // Music Method
            sb.AppendLine("    public static void PlayMusic(EMusicEnums music, bool isMain = false, float delay = 0f)");
            sb.AppendLine("    {");
            //sb.AppendLine("        if (AudioManager.Instance == null) return;");
            sb.AppendLine("        AudioManager.PlayMusic(music.ToString(), isMain, delay);");
            sb.AppendLine("    }");

            // SFX Method
            sb.AppendLine("    public static void PlaySfx(ESfxEnums sfx, Vector3? position = null, float delay = 0f)");
            sb.AppendLine("    {");
            //sb.AppendLine("        if (AudioManager.Instance == null) return;");
            sb.AppendLine("        AudioManager.PlaySfx(sfx.ToString(), position, delay);");
            sb.AppendLine("    }");

            sb.AppendLine("}");

            File.WriteAllText(GeneratedPath, sb.ToString());
            
            AssetDatabase.Refresh();
            Logger.Log($"Audio Enums Generated at {GeneratedPath}");
        }

        private void AppendEnum(StringBuilder sb, string enumName, string[] names)
        {
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");
            
            if (names.Length == 0)
            {
                sb.AppendLine("        None = 0,");
            }
            else
            {
                for (int i = 0; i < names.Length; i++)
                {
                    string safe = MakeSafeEnumName(names[i]);
                    sb.AppendLine($"        {safe} = {i},");
                }
            }
            
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        private static string MakeSafeEnumName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "None";
            var sb = new StringBuilder();

            if (!char.IsLetter(raw[0]) && raw[0] != '_')
            {
                sb.Append('_');
            }

            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }

            return sb.ToString();
        }
        
        private string SanitizeName(string value)
        {
            // Remove spaces and special chars, ensure valid C# identifier
            return Regex.Replace(value, @"[^a-zA-Z0-9_]", "");
        }

        
        public ILogOrigin Logger => this;
        public ELogOrigin LogOrigin => ELogOrigin.AudioManager;
    }
}

#endif