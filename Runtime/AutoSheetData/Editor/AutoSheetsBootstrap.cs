using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using Ared.Core.AutoSheetData.Data;

namespace Ared.Core.AutoSheetData.Editor
{
    [InitializeOnLoad]
    internal static class AutoSheetsBootstrap
    {
        static AutoSheetsBootstrap()
        {
            EditorApplication.delayCall += EnsureCollectionsExist;
        }

        private static void EnsureCollectionsExist()
        {
            var guids = AssetDatabase.FindAssets("t:AutoSheetData.Data.SpreadsheetConfig");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<SpreadsheetConfig>(path);
                if (cfg == null || !cfg.autoCreateCollections) continue;

                foreach (var s in cfg.Sheets.Where(x => x.selected))
                {
                    var collFullName = $"{cfg.generatedNamespace}.{s.collectionClassName}";
                    var collType = ResolveType(collFullName);
                    if (collType == null) continue; // not compiled yet

                    var collectionsDir = GetCollectionsDir(cfg, s);
                    CodeGenerator.EnsureFolder(collectionsDir);

                    if (string.IsNullOrEmpty(s.expectedAssetPath))
                        s.expectedAssetPath = $"{collectionsDir}/{s.collectionClassName}.asset";

                    if (!File.Exists(s.expectedAssetPath))
                    {
                        var inst = ScriptableObject.CreateInstance(collType);
                        AssetDatabase.CreateAsset(inst, s.expectedAssetPath);
                        Debug.Log($"<color=#00FF00>[AutoSheetData]</color> Created collection asset: {s.expectedAssetPath}");
                    }
                }

                EditorUtility.SetDirty(cfg);
            }
            AssetDatabase.SaveAssets();
        }

        private static string GetConfigDir(SpreadsheetConfig cfg)
        {
            var assetPath = AssetDatabase.GetAssetPath(cfg);
            var dir = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            return dir.StartsWith("Assets/") || dir == "Assets" ? dir : "Assets";
        }

        private static string SanitizeFolder(string name)
            => SchemaUtils.SanitizeClassName(name);

        private static string GetSheetBaseDir(SpreadsheetConfig cfg, SpreadsheetConfig.SheetSelection s)
            => $"{GetConfigDir(cfg)}/{SanitizeFolder(s.sheetName)}";

        private static string GetCollectionsDir(SpreadsheetConfig cfg, SpreadsheetConfig.SheetSelection s)
            => $"{GetSheetBaseDir(cfg, s)}";

        private static System.Type ResolveType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}