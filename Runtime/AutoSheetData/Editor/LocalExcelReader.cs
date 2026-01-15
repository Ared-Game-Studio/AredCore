using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System. Text;
using System.Text.RegularExpressions;

namespace Ared.Core.AutoSheetData.Editor
{
    /// <summary>
    /// Reads local Excel (. xlsx) files without external dependencies.
    /// Uses the Open XML format (xlsx is a ZIP archive containing XML files).
    /// </summary>
    internal static class LocalExcelReader
    {
        /// <summary>
        /// Reads all data from a specific sheet in an Excel file.
        /// </summary>
        /// <param name="filePath">Full path to the . xlsx file. </param>
        /// <param name="sheetName">Name of the sheet tab to read.</param>
        /// <returns>List of rows, where each row is a list of cell values as strings.</returns>
        public static List<List<string>> ReadSheet(string filePath, string sheetName)
        {
            ValidateInputs(filePath, sheetName);

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode. Read))
            {
                var sharedStrings = LoadSharedStrings(archive);
                int sheetIndex = GetSheetIndex(archive, sheetName);
                var sheetPath = GetSheetPath(archive, sheetIndex);
                return ReadSheetXml(archive, sheetPath, sharedStrings);
            }
        }

        /// <summary>
        /// Gets all sheet names from an Excel file.
        /// </summary>
        /// <param name="filePath">Full path to the . xlsx file.</param>
        /// <returns>List of sheet names. </returns>
        public static List<string> GetSheetNames(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is empty.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel file not found:  {filePath}");

            var sheetNames = new List<string>();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                var workbookEntry = GetEntry(archive, "xl/workbook.xml");
                if (workbookEntry == null)
                    throw new Exception("Invalid xlsx file: workbook. xml not found.");

                using (var stream = workbookEntry.Open())
                using (var reader = new StreamReader(stream))
                {
                    var content = reader. ReadToEnd();
                    var matches = Regex.Matches(content, @"<sheet[^>]+name=""([^""]+)""", RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            sheetNames.Add(match.Groups[1].Value);
                        }
                    }
                }
            }

            return sheetNames;
        }

        private static void ValidateInputs(string filePath, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is empty.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel file not found: {filePath}");
            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentException("Sheet name is empty.");
        }

        private static ZipArchiveEntry GetEntry(ZipArchive archive, string path)
        {
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName. Equals(path, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }
            return null;
        }

        private static List<string> LoadSharedStrings(ZipArchive archive)
        {
            var strings = new List<string>();
            var entry = GetEntry(archive, "xl/sharedStrings.xml");

            if (entry == null)
                return strings;

            using (var stream = entry. Open())
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();

                int pos = 0;
                while (true)
                {
                    int siStart = content.IndexOf("<si", pos);
                    if (siStart < 0) break;

                    int siEnd = content.IndexOf("</si>", siStart);
                    if (siEnd < 0)
                    {
                        int selfClose = content.IndexOf("/>", siStart);
                        if (selfClose < 0) break;
                        strings.Add("");
                        pos = selfClose + 2;
                        continue;
                    }

                    string siContent = content.Substring(siStart, siEnd - siStart + 5);

                    var sb = new StringBuilder();
                    var tMatches = Regex.Matches(siContent, @"<t[^>]*>([^<]*)</t>");
                    foreach (Match tMatch in tMatches)
                    {
                        sb.Append(tMatch.Groups[1].Value);
                    }

                    strings.Add(DecodeXmlEntities(sb.ToString()));
                    pos = siEnd + 5;
                }
            }

            return strings;
        }

        private static int GetSheetIndex(ZipArchive archive, string sheetName)
        {
            var workbookEntry = GetEntry(archive, "xl/workbook.xml");
            if (workbookEntry == null)
                throw new Exception("Invalid xlsx file: workbook.xml not found.");

            using (var stream = workbookEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                var matches = Regex.Matches(content, @"<sheet[^>]+name=""([^""]+)""", RegexOptions.IgnoreCase);
                int index = 0;
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var name = match.Groups[1]. Value;
                        if (name. Equals(sheetName, StringComparison.OrdinalIgnoreCase))
                        {
                            return index;
                        }
                    }
                    index++;
                }
            }

            throw new Exception($"Sheet '{sheetName}' not found in workbook.");
        }

        private static string GetSheetPath(ZipArchive archive, int sheetIndex)
        {
            var directPath = $"xl/worksheets/sheet{sheetIndex + 1}.xml";
            if (GetEntry(archive, directPath) != null)
                return directPath;

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) &&
                    entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    return entry.FullName;
                }
            }

            throw new Exception($"Could not find worksheet file for sheet index {sheetIndex}");
        }

        private static List<List<string>> ReadSheetXml(ZipArchive archive, string sheetPath, List<string> sharedStrings)
        {
            var entry = GetEntry(archive, sheetPath);
            if (entry == null)
                throw new Exception($"Sheet file not found: {sheetPath}");

            var cellData = new Dictionary<int, Dictionary<int, string>>();
            int maxRow = 0;
            int maxCol = 0;

            using (var stream = entry. Open())
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();

                int sheetDataStart = content.IndexOf("<sheetData");
                int sheetDataEnd = content.IndexOf("</sheetData>");

                if (sheetDataStart < 0 || sheetDataEnd < 0)
                {
                    return new List<List<string>>();
                }

                string sheetData = content.Substring(sheetDataStart, sheetDataEnd - sheetDataStart + 12);

                int rowPos = 0;
                while (true)
                {
                    int rowStart = sheetData.IndexOf("<row", rowPos);
                    if (rowStart < 0) break;

                    int rowEnd;
                    int tagEnd = sheetData.IndexOf(">", rowStart);
                    bool isSelfClosing = tagEnd > 0 && sheetData[tagEnd - 1] == '/';

                    if (isSelfClosing)
                    {
                        rowEnd = tagEnd + 1;
                    }
                    else
                    {
                        int fullCloseCheck = sheetData.IndexOf("</row>", rowStart);
                        if (fullCloseCheck < 0) break;
                        rowEnd = fullCloseCheck + 6;
                    }

                    string rowXml = sheetData. Substring(rowStart, rowEnd - rowStart);

                    var rowNumMatch = Regex.Match(rowXml, @"<row[^>]*\sr=""(\d+)""");
                    if (! rowNumMatch.Success)
                        rowNumMatch = Regex.Match(rowXml, @"<row[^>]*r=""(\d+)""");

                    int rowNum = 1;
                    if (rowNumMatch.Success)
                    {
                        rowNum = int. Parse(rowNumMatch.Groups[1].Value);
                    }

                    if (rowNum > maxRow) maxRow = rowNum;

                    if (! cellData.ContainsKey(rowNum))
                        cellData[rowNum] = new Dictionary<int, string>();

                    // Find all cells - look for <c followed by space, newline, or attribute
                    int cellPos = 0;
                    while (true)
                    {
                        int cellStart = FindCellStart(rowXml, cellPos);
                        if (cellStart < 0) break;

                        int cellEnd = FindCellEnd(rowXml, cellStart);
                        if (cellEnd < 0) break;

                        string cellXml = rowXml.Substring(cellStart, cellEnd - cellStart);

                        var refMatch = Regex.Match(cellXml, @"r=""([A-Z]+\d+)""", RegexOptions.IgnoreCase);
                        if (! refMatch.Success)
                        {
                            cellPos = cellEnd;
                            continue;
                        }

                        string cellRef = refMatch.Groups[1].Value;
                        var (cellRow, cellCol) = ParseCellReference(cellRef);

                        if (cellCol > maxCol) maxCol = cellCol;

                        var typeMatch = Regex.Match(cellXml, @"t=""([^""]+)""");
                        string cellType = typeMatch.Success ? typeMatch.Groups[1]. Value : "";

                        string cellValue = "";
                        var valueMatch = Regex.Match(cellXml, @"<v>([^<]*)</v>");
                        if (valueMatch. Success)
                        {
                            cellValue = valueMatch.Groups[1].Value;
                        }

                        if (cellType == "s" && int.TryParse(cellValue, out var ssIndex))
                        {
                            cellValue = ssIndex < sharedStrings.Count ? sharedStrings[ssIndex] :  "";
                        }
                        else if (cellType == "inlineStr")
                        {
                            var inlineMatch = Regex.Match(cellXml, @"<t[^>]*>([^<]*)</t>");
                            if (inlineMatch.Success)
                            {
                                cellValue = inlineMatch.Groups[1].Value;
                            }
                        }

                        cellData[rowNum][cellCol] = DecodeXmlEntities(cellValue);
                        cellPos = cellEnd;
                    }

                    rowPos = rowEnd;
                }
            }

            // Convert to list format
            var rows = new List<List<string>>();
            for (int r = 1; r <= maxRow; r++)
            {
                var row = new List<string>();
                for (int c = 1; c <= maxCol; c++)
                {
                    if (cellData.TryGetValue(r, out var cols) && cols.TryGetValue(c, out var val))
                        row.Add(val);
                    else
                        row.Add("");
                }

                if (r == 1 || row. Exists(cell => ! string.IsNullOrEmpty(cell)))
                    rows.Add(row);
            }

            return rows;
        }

        private static int FindCellStart(string xml, int startPos)
        {
            int pos = startPos;
            while (pos < xml.Length)
            {
                int idx = xml.IndexOf("<c", pos);
                if (idx < 0) return -1;

                // Check what follows <c - must be space, newline, tab, or > for it to be a cell element
                int nextCharIdx = idx + 2;
                if (nextCharIdx < xml.Length)
                {
                    char nextChar = xml[nextCharIdx];
                    if (nextChar == ' ' || nextChar == '\r' || nextChar == '\n' || nextChar == '\t' || nextChar == '>')
                    {
                        return idx;
                    }
                }

                pos = idx + 1;
            }
            return -1;
        }

        private static int FindCellEnd(string xml, int cellStart)
        {
            int tagEnd = xml.IndexOf(">", cellStart);
            if (tagEnd < 0) return -1;

            bool isSelfClosing = xml[tagEnd - 1] == '/';
            if (isSelfClosing)
            {
                return tagEnd + 1;
            }

            int fullClose = xml.IndexOf("</c>", cellStart);
            if (fullClose >= 0)
            {
                return fullClose + 4;
            }

            return -1;
        }

        private static (int row, int col) ParseCellReference(string cellRef)
        {
            var match = Regex.Match(cellRef, @"^([A-Z]+)(\d+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
                return (1, 1);

            var colLetters = match.Groups[1]. Value.ToUpperInvariant();
            var rowNum = int.Parse(match. Groups[2].Value);

            int colNum = 0;
            foreach (char c in colLetters)
            {
                colNum = colNum * 26 + (c - 'A' + 1);
            }

            return (rowNum, colNum);
        }

        private static string DecodeXmlEntities(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'");
        }
    }
}