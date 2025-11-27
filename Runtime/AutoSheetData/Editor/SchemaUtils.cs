using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Ared.Core.AutoSheetData.Data;
using UnityEngine;

namespace Ared.Core.AutoSheetData.Editor
{
    internal static class SchemaUtils
    {
        internal static string ToCSharpType(EColumnType t) => t switch
        {
            EColumnType.Int => "int",
            EColumnType.Float => "float",
            _ => "string"
        };

        internal static object ConvertValue(string raw, EColumnType t)
        {
            try
            {
                switch (t)
                {
                    case EColumnType.Int:
                        if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) return i;
                        return 0;
                    case EColumnType.Float:
                        if (float.TryParse((raw ?? "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var f)) return f;
                        return 0f;
                    default:
                        return raw ?? string.Empty;
                }
            }
            catch { return t == EColumnType.String ? raw ?? string.Empty : 0; }
        }

        internal static string SanitizeClassName(string input)
        {
            var pascal = ToPascalCase(input);
            return RemoveInvalidChars(pascal, true);
        }

        internal static string SanitizeFieldName(string input)
        {
            var camel = ToCamelCase(input);
            camel = RemoveInvalidChars(camel, true);
            if (string.IsNullOrEmpty(camel)) camel = "field";
            return camel;
        }

        internal static List<string> MakeUnique(IEnumerable<string> names)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>();
            foreach (var n in names)
            {
                var name = n;
                int suffix = 1;
                while (!seen.Add(name)) name = $"{n}_{suffix++}";
                result.Add(name);
            }
            return result;
        }

        internal static string ComputeSchemaHash(IReadOnlyList<string> headers, IReadOnlyList<EColumnType> types)
        {
            var joined = string.Join("|", headers) + "||" + string.Join("|", types.Select(t => t.ToString()));
            var bytes = Encoding.UTF8.GetBytes(joined);
            using (var sha = new System.Security.Cryptography.SHA1Managed())
            {
                var hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Unnamed";
            var parts = SplitWords(input);
            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1) sb.Append(p.Substring(1).ToLowerInvariant());
            }
            return sb.Length == 0 ? "Unnamed" : sb.ToString();
        }

        private static string ToCamelCase(string input)
        {
            var pascal = ToPascalCase(input);
            if (string.IsNullOrEmpty(pascal)) return "unnamed";
            return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }

        private static string RemoveInvalidChars(string input, bool firstCharMustBeLetter)
        {
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (i == 0 && firstCharMustBeLetter)
                {
                    if (char.IsLetter(c) || c == '_') sb.Append(c);
                    else sb.Append('_');
                }
                else
                {
                    if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                    else sb.Append('_');
                }
            }
            return sb.ToString();
        }

        private static IEnumerable<string> SplitWords(string input)
        {
            var sb = new StringBuilder();
            foreach (var c in input) sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}