using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ared.Core.AutoSheetData.Data
{
    [CreateAssetMenu(menuName = "AutoSheetData/Spreadsheet Config", fileName = "SpreadsheetConfig")]
    public class SpreadsheetConfig : ScriptableObject
    {
        [Header("Google Sheets (Public)")]
        [Tooltip("The ID part from https://docs.google.com/spreadsheets/d/{THIS_PART}/")]
        public string spreadsheetId;
        
        [Header("Generation")]
        [Tooltip("C# namespace for generated row and collection classes.")]
        public string generatedNamespace = "AutoSheetData.Generated";

        [Tooltip("Automatically create collection assets after scripts compile.")]
        public bool autoCreateCollections = true;

        [Serializable]
        public class ColumnSetting
        {
            public string columnName;
            public EColumnType type = EColumnType.String;
        }

        [Serializable]
        public class SheetSelection
        {
            public string sheetName;
            public bool selected;

            // Column type settings defined in Unity
            public List<ColumnSetting> columns = new List<ColumnSetting>();

            // Generated type names
            public string rowClassName;
            public string collectionClassName;

            // For regeneration bookkeeping (optional)
            public string lastSchemaHash;

            // Expected collection asset path
            public string expectedAssetPath;
        }

        public List<SheetSelection> Sheets = new List<SheetSelection>();
    }
}