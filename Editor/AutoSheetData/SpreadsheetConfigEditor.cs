using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ared.AutoSheetData.Data;
using UnityEditor;
using UnityEngine;

namespace Ared.AutoSheetData.Editor
{
    [CustomEditor(typeof(SpreadsheetConfig))]
    public class SpreadsheetConfigEditor : UnityEditor.Editor
    {
        private SpreadsheetConfig _config;
        private bool _isBusy;
        private string _status = "";
        private Vector2 _sheetsScroll;
        private string _urlInput = "";

        public override void OnInspectorGUI()
        {
            _config = (SpreadsheetConfig)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Google Sheet URL", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _urlInput = EditorGUILayout.TextField("Public URL", _urlInput);
                if (GUILayout.Button("Parse", GUILayout.Width(80)))
                {
                    if (TryExtractSpreadsheetIdFromUrl(_urlInput, out var id))
                    {
                        _config.spreadsheetId = id;
                        EditorUtility.SetDirty(_config);
                        _status = "Parsed Spreadsheet ID.";
                    }
                    else
                    {
                        _status = "Failed to parse Spreadsheet ID from URL.";
                        LogWarning("[AutoSheetData] Could not parse Spreadsheet ID from provided URL.");
                    }
                }
            }
            
            EditorGUILayout.Space(20);

            DrawConfigFields();

            
            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(_isBusy);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Data")) _ = GenerateTypesAsync();
                if (GUILayout.Button("Create Collections")) CreateCollectionsNow();
                if (GUILayout.Button("Sync")) _ = SyncNowAsync();
            }
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_status, MessageType.None);
            }

            EditorGUILayout.Space(50);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Sheets", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ Add Sheet", GUILayout.Width(110)))
                    AddSheet();
            }

            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            _sheetsScroll = EditorGUILayout.BeginScrollView(_sheetsScroll, GUILayout.ExpandHeight(true));
            for (int idx = 0; idx < _config.Sheets.Count; idx++)
            {
                var s = _config.Sheets[idx];

                EditorGUILayout.BeginVertical("box");
                using (new EditorGUILayout.HorizontalScope())
                {
                    s.selected = EditorGUILayout.Toggle(s.selected, GUILayout.Width(20));

                    EditorGUI.BeginChangeCheck();
                    s.sheetName = EditorGUILayout.TextField("Sheet Name", s.sheetName, GUILayout.MinWidth(300));
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Refresh generated names and expected asset path when sheet name changes
                        var (rowName, collName) = CodeGenerator.ComputeTypeNames(s.sheetName);
                        s.rowClassName = rowName;
                        s.collectionClassName = collName;
                        s.expectedAssetPath = BuildCollectionAssetPath(_config, s);
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        _config.Sheets.RemoveAt(idx);
                        idx--;
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                }

                // Paths preview (read-only)
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Generated Path", GetGeneratedDir(_config, s));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Collections Path", GetCollectionsDir(_config, s));
                }

                // Quick actions row
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(_isBusy || string.IsNullOrWhiteSpace(_config.spreadsheetId) || string.IsNullOrWhiteSpace(s.sheetName));
                    if (GUILayout.Button("Load/Refresh Columns", GUILayout.Width(180)))
                        _ = LoadColumnsAsync(s);
                    EditorGUI.EndDisabledGroup();

                    GUILayout.Label($"Columns: {s.columns.Count}", GUILayout.Width(120));
                    GUILayout.FlexibleSpace();
                }

                // Columns list with enum type selection
                if (s.columns.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < s.columns.Count; i++)
                    {
                        var col = s.columns[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(col.columnName, GUILayout.MinWidth(120));
                            col.type = (EColumnType)EditorGUILayout.EnumPopup(col.type, GUILayout.MaxWidth(120));
                            //GUILayout.FlexibleSpace();
                            // if (GUILayout.Button("X", GUILayout.Width(24)))
                            // {
                            //     s.columns.RemoveAt(i);
                            //     i--;
                            //     continue;
                            // }
                        }
                        // Draw a horizontal separator between columns
                        if (i < s.columns.Count - 1)
                            DrawSeparator();
                    }
                    EditorGUI.indentLevel--;
                }

                // Manual add column row
                // using (new EditorGUILayout.HorizontalScope())
                // {
                //     if (GUILayout.Button("+ Add Column", GUILayout.Width(120)))
                //     {
                //         s.columns.Add(new SpreadsheetConfig.ColumnSetting
                //         {
                //             columnName = "NewColumn",
                //             type = EColumnType.String
                //         });
                //     }
                //     GUILayout.FlexibleSpace();
                // }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Do NOT call base.OnInspectorGUI() to avoid duplicate fields rendering

            if (GUI.changed)
                EditorUtility.SetDirty(_config);
        }

        private void DrawConfigFields()
        {
            _config.spreadsheetId = EditorGUILayout.TextField("Spreadsheet ID", _config.spreadsheetId);
            _config.generatedNamespace = EditorGUILayout.TextField("Namespace", _config.generatedNamespace);
            _config.autoCreateCollections = EditorGUILayout.Toggle("Auto Create Collections", _config.autoCreateCollections);
        }

        private void AddSheet()
        {
            var s = new SpreadsheetConfig.SheetSelection
            {
                sheetName = "NewSheet",
                selected = true
            };
            var (rowName, collName) = CodeGenerator.ComputeTypeNames(s.sheetName);
            s.rowClassName = rowName;
            s.collectionClassName = collName;
            s.expectedAssetPath = BuildCollectionAssetPath(_config, s);
            _config.Sheets.Add(s);
            EditorUtility.SetDirty(_config);
        }

        private async Task LoadColumnsAsync(SpreadsheetConfig.SheetSelection sheet)
        {
            try
            {
                _isBusy = true;
                _status = $"Loading columns for '{sheet.sheetName}'...";
                Repaint();

                var rows = await PublicSheetsClient.FetchValuesCsvByNameAsync(_config.spreadsheetId, sheet.sheetName);
                var headers = rows.Count > 0 ? rows[0] : new List<string>();
                var dataRows = rows.Count > 1 ? rows.Skip(1).ToList() : new List<List<string>>();

                // Initialize or refresh the Columns list to match headers order with auto-detected types
                var existing = sheet.columns.ToDictionary(c => c.columnName, c => c, StringComparer.OrdinalIgnoreCase);
                var newList = new List<SpreadsheetConfig.ColumnSetting>();
                for (int h = 0; h < headers.Count; h++)
                {
                    var header = headers[h];
                    if (string.IsNullOrEmpty(header)) continue;

                    // Find the first non-empty sample value for this column
                    string sample = null;
                    foreach (var row in dataRows)
                    {
                        if (h < row.Count)
                        {
                            var cell = row[h]?.Trim();
                            if (!string.IsNullOrEmpty(cell))
                            {
                                sample = cell;
                                break;
                            }
                        }
                    }

                    var detectedType = DetectColumnType(sample);
                    if (existing.TryGetValue(header, out var col))
                    {
                        col.type = detectedType;
                        newList.Add(col);
                    }
                    else
                    {
                        newList.Add(new SpreadsheetConfig.ColumnSetting
                        {
                            columnName = header,
                            type = detectedType
                        });
                    }
                }
                sheet.columns = newList;

                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                _status = $"Columns loaded for '{sheet.sheetName}' (types auto-detected).";
                Log($"[AutoSheetData] Loaded {sheet.columns.Count} columns for '{sheet.sheetName}'. Types auto-detected from first data value per column.");
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load columns for '{sheet.sheetName}': " + ex.Message);
            }
            finally
            {
                _isBusy = false;
                Repaint();
            }
        }

        private static EColumnType DetectColumnType(string sample)
        {
            if (string.IsNullOrWhiteSpace(sample))
                return EColumnType.String;

            // Normalize decimal comma to dot for parsing
            var norm = sample.Trim();

            // If it looks like a float (has decimal separator or exponent), try float first
            bool looksFloat = norm.Contains(".") || norm.Contains(",") || norm.IndexOf('e') >= 0 || norm.IndexOf('E') >= 0;
            if (looksFloat)
            {
                var nf = norm.Replace(',', '.');
                if (float.TryParse(nf, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    return EColumnType.Float;
            }

            // Try integer
            // Allow leading +/-, no thousands separators
            if (int.TryParse(norm, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return EColumnType.Int;

            // As a fallback, maybe it's a float without obvious markers (rare), attempt parse once more
            {
                var nf = norm.Replace(',', '.');
                if (float.TryParse(nf, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    return EColumnType.Float;
            }

            return EColumnType.String;
        }

        private async Task GenerateTypesAsync()
        {
            var selected = _config.Sheets.Where(s => s.selected).ToList();
            if (!selected.Any())
                selected = _config.Sheets.ToList();

            if (!selected.Any())
            {
                ShowError("No sheets defined. Add a sheet first.");
                return;
            }

            try
            {
                _isBusy = true;
                _status = "Generating code from sheets...";
                Repaint();

                foreach (var s in selected)
                {
                    // Ensure names are computed
                    if (string.IsNullOrWhiteSpace(s.rowClassName) || string.IsNullOrWhiteSpace(s.collectionClassName))
                    {
                        var (rowName, collName) = CodeGenerator.ComputeTypeNames(s.sheetName);
                        s.rowClassName = rowName;
                        s.collectionClassName = collName;
                    }

                    // If no columns configured, try to load headers and auto-detect types
                    if (s.columns == null || s.columns.Count == 0)
                        await LoadColumnsAsync(s);

                    if (s.columns == null || s.columns.Count == 0)
                    {
                        LogWarning($"[AutoSheetData] No columns found for sheet {s.sheetName}. Skipping generation.");
                        continue;
                    }

                    var headers = s.columns.Select(c => c.columnName).ToList();
                    var types = s.columns.Select(c => c.type).ToList();
                    var cols = CodeGenerator.BuildColumnsFromConfig(headers, types);
                    var schemaHash = SchemaUtils.ComputeSchemaHash(headers, types);

                    var generatedDir = GetGeneratedDir(_config, s);
                    CodeGenerator.EnsureFolder(generatedDir);

                    var rowCode = CodeGenerator.BuildRowClassCode(_config.generatedNamespace, s.rowClassName, cols);
                    CodeGenerator.WriteCodeFile(generatedDir, $"{s.rowClassName}.cs", rowCode);

                    var collCode = CodeGenerator.BuildCollectionClassCode(_config.generatedNamespace, s.collectionClassName, s.rowClassName);
                    CodeGenerator.WriteCodeFile(generatedDir, $"{s.collectionClassName}.cs", collCode);

                    s.lastSchemaHash = schemaHash;
                    s.expectedAssetPath = BuildCollectionAssetPath(_config, s);

                    Log($"[AutoSheetData] Generated: {generatedDir}/{s.rowClassName}.cs, {generatedDir}/{s.collectionClassName}.cs");
                }

                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                _status = "Code generated. Unity is compiling scripts. Use 'Create Collections Now' then 'Sync Now'.";
                Log("[AutoSheetData] Code generation complete.");
            }
            catch (Exception ex)
            {
                ShowError("Code generation failed: " + ex.Message);
            }
            finally
            {
                _isBusy = false;
                Repaint();
            }
        }

        private void CreateCollectionsNow()
        {
            try
            {
                var selected = _config.Sheets.Where(s => s.selected).ToList();
                if (!selected.Any()) selected = _config.Sheets.ToList();

                if (!selected.Any())
                {
                    ShowError("No sheets defined. Add a sheet first.");
                    return;
                }

                foreach (var s in selected)
                {
                    var collType = ResolveType($"{_config.generatedNamespace}.{s.collectionClassName}");
                    if (collType == null)
                    {
                        LogWarning($"[AutoSheetData] Collection type not compiled yet: {s.collectionClassName}");
                        continue;
                    }

                    var collectionsDir = GetCollectionsDir(_config, s);
                    CodeGenerator.EnsureFolder(collectionsDir);

                    if (string.IsNullOrEmpty(s.expectedAssetPath))
                        s.expectedAssetPath = $"{collectionsDir}/{s.collectionClassName}.asset";

                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(s.expectedAssetPath);
                    if (asset == null)
                    {
                        var instance = ScriptableObject.CreateInstance(collType);
                        AssetDatabase.CreateAsset(instance, s.expectedAssetPath);
                        Log($"[AutoSheetData] Created collection asset: {s.expectedAssetPath}");
                    }
                }

                AssetDatabase.SaveAssets();
                _status = "Collections created (if missing).";
                EditorUtility.SetDirty(_config);
            }
            catch (Exception ex)
            {
                ShowError("Failed to create collections: " + ex.Message);
            }
        }

        private async Task SyncNowAsync()
        {
            try
            {
                _isBusy = true;
                _status = "Syncing data...";
                Repaint();

                var selected = _config.Sheets.Where(s => s.selected).ToList();
                if (!selected.Any()) selected = _config.Sheets.ToList();

                if (!selected.Any())
                {
                    ShowError("No sheets defined. Add a sheet first.");
                    return;
                }

                foreach (var s in selected)
                {
                    var rowType = ResolveType($"{_config.generatedNamespace}.{s.rowClassName}");
                    var collType = ResolveType($"{_config.generatedNamespace}.{s.collectionClassName}");
                    if (rowType == null || collType == null)
                    {
                        LogWarning($"[AutoSheetData] Types not compiled for sheet {s.sheetName}. Generate Types first.");
                        continue;
                    }

                    var collectionsDir = GetCollectionsDir(_config, s);
                    CodeGenerator.EnsureFolder(collectionsDir);

                    if (string.IsNullOrEmpty(s.expectedAssetPath))
                        s.expectedAssetPath = $"{collectionsDir}/{s.collectionClassName}.asset";

                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(s.expectedAssetPath);
                    if (asset == null)
                    {
                        var instance = ScriptableObject.CreateInstance(collType);
                        AssetDatabase.CreateAsset(instance, s.expectedAssetPath);
                        asset = instance;
                    }

                    // Fetch rows and headers using the correct endpoint (by sheet name)
                    var rows = await PublicSheetsClient.FetchValuesCsvByNameAsync(_config.spreadsheetId, s.sheetName);
                    if (rows.Count == 0)
                    {
                        LogWarning($"[AutoSheetData] No data in sheet '{s.sheetName}'.");
                        continue;
                    }
                    var headers = rows[0];
                    var dataRows = rows.Skip(1).ToList(); // Types are defined in Unity config

                    // Map headers to indices
                    var headerToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < headers.Count; i++)
                        if (!string.IsNullOrEmpty(headers[i]) && !headerToIndex.ContainsKey(headers[i]))
                            headerToIndex[headers[i]] = i;

                    // Ensure configured columns exist
                    if (s.columns == null || s.columns.Count == 0)
                        await LoadColumnsAsync(s);
                    if (s.columns == null || s.columns.Count == 0)
                    {
                        LogWarning($"[AutoSheetData] No configured columns for {s.sheetName}. Skipping.");
                        continue;
                    }

                    // Fill collection.Items
                    var itemsField = collType.GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
                    var listInstance = itemsField.GetValue(asset) as System.Collections.IList;
                    listInstance.Clear();

                    // Prepare mapping from configured columns to row fields
                    var fieldMap = new List<(FieldInfo field, int csvIndex, EColumnType colType)>();
                    foreach (var c in s.columns)
                    {
                        var fieldName = SchemaUtils.SanitizeFieldName(c.columnName);
                        var fi = rowType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                        if (fi == null)
                        {
                            LogWarning($"[AutoSheetData] Field '{fieldName}' not found in {rowType.Name} (from column '{c.columnName}').");
                            continue;
                        }
                        if (!headerToIndex.TryGetValue(c.columnName, out var colIdx))
                        {
                            LogWarning($"[AutoSheetData] Column '{c.columnName}' not found in sheet '{s.sheetName}'.");
                            continue;
                        }
                        fieldMap.Add((fi, colIdx, c.type));
                    }

                    foreach (var rowVals in dataRows)
                    {
                        var rowObj = Activator.CreateInstance(rowType);
                        foreach (var map in fieldMap)
                        {
                            var raw = map.csvIndex < rowVals.Count ? rowVals[map.csvIndex] : "";
                            var val = SchemaUtils.ConvertValue(raw, map.colType);
                            map.field.SetValue(rowObj, val);
                        }
                        listInstance.Add(rowObj);
                    }

                    EditorUtility.SetDirty(asset);
                    Log($"[AutoSheetData] Synced {listInstance.Count} rows for '{s.sheetName}'.");
                }

                AssetDatabase.SaveAssets();
                _status = "Sync complete.";
            }
            catch (Exception ex)
            {
                ShowError("Sync failed: " + ex.Message);
            }
            finally
            {
                _isBusy = false;
                Repaint();
            }
        }

        // Path helpers (per-sheet under config directory)
        private static string GetConfigDir(SpreadsheetConfig cfg)
        {
            var assetPath = AssetDatabase.GetAssetPath(cfg);
            var dir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            return dir.StartsWith("Assets/") || dir == "Assets" ? dir : "Assets";
        }

        private static string SanitizeFolder(string name)
        {
            // Reuse class-name sanitizer as a safe folder name
            return SchemaUtils.SanitizeClassName(name);
        }

        private static string GetSheetBaseDir(SpreadsheetConfig cfg, SpreadsheetConfig.SheetSelection s)
        {
            var baseDir = GetConfigDir(cfg);
            var sheetFolder = SanitizeFolder(s.sheetName);
            return $"{baseDir}/{sheetFolder}";
        }

        private static string GetGeneratedDir(SpreadsheetConfig cfg, SpreadsheetConfig.SheetSelection s)
            => $"{GetSheetBaseDir(cfg, s)}/Generated";

        private static string GetCollectionsDir(SpreadsheetConfig cfg, SpreadsheetConfig.SheetSelection s)
            => $"{GetSheetBaseDir(cfg, s)}";

        private static string BuildCollectionAssetPath(SpreadsheetConfig cfg, SpreadsheetConfig.SheetSelection s)
            => $"{GetCollectionsDir(cfg, s)}/{s.collectionClassName}.asset";

        private static Type ResolveType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private bool TryExtractSpreadsheetIdFromUrl(string url, out string id)
        {
            id = null;
            if (string.IsNullOrWhiteSpace(url)) return false;

            // Extract ID from .../spreadsheets/d/{ID}/...
            var m = Regex.Match(url, @"docs\.google\.com\/spreadsheets\/d\/([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
            if (m.Success && m.Groups.Count > 1)
            {
                id = m.Groups[1].Value;
            }

            return !string.IsNullOrEmpty(id);
        }
        
        private static void DrawSeparator(float thickness = 1f, float padding = 6f)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + padding));
            rect.height = thickness;
            rect.y += padding * 0.5f;
            rect.xMin += 12f; // slight indent inside the box
            rect.xMax -= 6f;

            var color = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, 1f)
                : new Color(0.65f, 0.65f, 0.65f, 1f);

            EditorGUI.DrawRect(rect, color);
        }
        
        
        // Logging wrappers:
        
        private const string GreenPrefix = "<color=#00FF00>[AutoSheetData]</color>";

        private void ShowError(string msg)
        {
            _status = msg;
            LogError(msg);
        }
        
        private static void Log(string message) => Debug.Log($"{GreenPrefix} {message}");
        private static void LogWarning(string message) => Debug.LogWarning($"{GreenPrefix} {message}");
        private static void LogError(string message) => Debug.LogError($"{GreenPrefix} {message}");
    }
}