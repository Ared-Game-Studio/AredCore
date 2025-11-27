using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ared.Core.AutoSheetData.Editor
{
    internal static class PublicSheetsClient
    {
        public static async Task<List<List<string>>> FetchValuesCsvByNameAsync(string spreadsheetId, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId))
                throw new ArgumentException("Spreadsheet ID is empty.");
            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentException("Sheet name is empty.");

            var url = $"https://docs.google.com/spreadsheets/d/{UnityWebRequest.EscapeURL(spreadsheetId)}/gviz/tq?tqx=out:csv&sheet={UnityWebRequest.EscapeURL(sheetName)}";
            var csv = await GetAsync(url);
            if (string.IsNullOrEmpty(csv))
                throw new Exception($"Empty CSV for sheet '{sheetName}'. Check public access and exact name.");

            return ParseCsv(csv);
        }

        private static async Task<string> GetAsync(string url)
        {
            using (var req = UnityWebRequest.Get(url))
            {
#if UNITY_2020_1_OR_NEWER
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
#else
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogError($"SheetsClient GET failed: {req.responseCode} {req.error}\nURL: {url}");
                    return null;
                }
                return req.downloadHandler.text;
            }
        }

        // Simple CSV parser supporting quoted cells and commas inside quotes
        private static List<List<string>> ParseCsv(string csv)
        {
            var rows = new List<List<string>>();
            var i = 0;
            int len = csv.Length;
            var cell = new StringBuilder();
            var row = new List<string>();
            bool inQuotes = false;

            while (i < len)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < len && csv[i + 1] == '"')
                        {
                            cell.Append('"'); // Escaped quote
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    else
                    {
                        cell.Append(c);
                        i++;
                        continue;
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                        i++;
                        continue;
                    }
                    else if (c == ',')
                    {
                        row.Add(cell.ToString());
                        cell.Length = 0;
                        i++;
                        continue;
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        // End of row
                        row.Add(cell.ToString());
                        cell.Length = 0;
                        rows.Add(row);
                        row = new List<string>();

                        if (c == '\r' && i + 1 < len && csv[i + 1] == '\n') i += 2;
                        else i++;
                        continue;
                    }
                    else
                    {
                        cell.Append(c);
                        i++;
                        continue;
                    }
                }
            }

            // Last cell/row
            row.Add(cell.ToString());
            rows.Add(row);

            // Remove trailing empty row if CSV ended with newline
            if (rows.Count > 0 && rows[^1].Count == 1 && string.IsNullOrEmpty(rows[^1][0]))
                rows.RemoveAt(rows.Count - 1);

            return rows;
        }
    }
}