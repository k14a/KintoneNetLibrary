using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

public static class KintoneValueConverter {
    public static object? ConvertToCSharp(JsonElement valueElement, KintoneFieldType fieldType, Type targetType) {
        if (valueElement.ValueKind == JsonValueKind.Null) {
            return null;
        }

        static bool IsStringListType(Type t) => t == typeof(IEnumerable<string>) || t == typeof(List<string>);

        string? str = valueElement.ValueKind == JsonValueKind.String ? valueElement.GetString() : null;

        // --- 日付型（DateTime / DateOnly / TimeOnly） ---
        if (fieldType is KintoneFieldType.DateTime or KintoneFieldType.Date or KintoneFieldType.Time) {
            return (targetType, fieldType) switch {
                // --- KintoneDateTime 型への変換 ---
                (Type t, KintoneFieldType.DateTime) when t == typeof(KintoneDateTime) => new KintoneDateTime(str, KintoneFieldType.DateTime),
                (Type t, KintoneFieldType.Date) when t == typeof(KintoneDateTime) => new KintoneDateTime(str, KintoneFieldType.Date),
                (Type t, KintoneFieldType.Time) when t == typeof(KintoneTimeOnly) => new KintoneTimeOnly(str, KintoneFieldType.Time),

                // --- DateTime 型（従来互換） ---
                (Type t, KintoneFieldType.DateTime) when t == typeof(DateTime) => DateTime.TryParse(str, out var dt) ? dt : DateTime.MinValue,
                (Type t, KintoneFieldType.DateTime) when t == typeof(DateTime?) => DateTime.TryParse(str, out var dt) ? dt : null,

                // --- DateOnly 型 ---
                (Type t, KintoneFieldType.Date) when t == typeof(DateOnly) => DateOnly.TryParse(str, out var d) ? d : DateOnly.MinValue,
                (Type t, KintoneFieldType.Date) when t == typeof(DateOnly?) => DateOnly.TryParse(str, out var d) ? d : null,

                // --- TimeOnly 型 ---
                (Type t, KintoneFieldType.Time) when t == typeof(TimeOnly) => TimeOnly.TryParse(str, out var to) ? to : TimeOnly.MinValue,
                (Type t, KintoneFieldType.Time) when t == typeof(TimeOnly?) => TimeOnly.TryParse(str, out var to) ? to : null,
                _ => throw new NotSupportedException($"Unsupported date/time conversion from {fieldType} to {targetType.Name}")
            };
        }

        // --- 通常型 ---
        return (targetType, valueElement.ValueKind) switch {
            (Type t, JsonValueKind.String) when t == typeof(string) => str,
            (Type t, JsonValueKind.String) when t == typeof(int) => int.TryParse(str, out var i) ? i : 0,
            (Type t, JsonValueKind.String) when t == typeof(int?) => int.TryParse(str, out var i) ? i : null,
            (Type t, JsonValueKind.Number) when t == typeof(int) => valueElement.TryGetInt32(out var i) ? i : 0,
            (Type t, JsonValueKind.Number) when t == typeof(int?) => valueElement.TryGetInt32(out var i) ? i : null,
            (Type t, JsonValueKind.String) when t == typeof(decimal) => decimal.TryParse(str, out var d) ? d : 0m,
            (Type t, JsonValueKind.String) when t == typeof(decimal?) => decimal.TryParse(str, out var d) ? d : null,
            (Type t, JsonValueKind.Number) when t == typeof(decimal) => valueElement.TryGetDecimal(out var d) ? d : 0m,
            (Type t, JsonValueKind.Number) when t == typeof(decimal?) => valueElement.TryGetDecimal(out var d) ? d : null,
            (Type t, JsonValueKind.Object) when t == typeof(KintoneUser) => JsonSerializer.Deserialize<KintoneUser>(valueElement.GetRawText()),
            (Type t, JsonValueKind.Array) when IsStringListType(t) => valueElement.EnumerateArray().Select(e => e.GetString()!).ToList(),
            (Type t, JsonValueKind.Array) when typeof(IList<KintoneFile>).IsAssignableFrom(t) =>
                valueElement.EnumerateArray()
                    .Select(f => new KintoneFile {
                        ContentType = f.GetProperty("contentType").GetString() ?? "",
                        FileKey = f.GetProperty("fileKey").GetString() ?? "",
                        Name = f.GetProperty("name").GetString() ?? "",
                        Size = long.TryParse(f.GetProperty("size").GetString(), out var size) ? size : 0
                    }).ToList(),
            (Type t, JsonValueKind.Array) when t == typeof(List<Dictionary<string, JsonElement>>) =>
                valueElement.EnumerateArray()
                    .Select(row => row.GetProperty("value").EnumerateObject().ToDictionary(p => p.Name, p => p.Value))
                    .ToList(),
            (Type t, JsonValueKind.Array) when typeof(IEnumerable<KintoneUser>).IsAssignableFrom(t) =>
                valueElement.EnumerateArray()
                    .Select(u => new KintoneUser {
                        Code = u.GetProperty("code").GetString() ?? "",
                        Name = u.GetProperty("name").GetString() ?? ""
                    }).ToList(),
            _ => throw new NotSupportedException($"Unsupported value conversion to {targetType.Name} from kind: {valueElement.ValueKind}")
        };
    }

    private static int? ParseNullableInt(JsonElement valueElement) {
        return valueElement.ValueKind switch {
            JsonValueKind.Number => valueElement.GetInt32(),
            JsonValueKind.String => ParseIntFromString(valueElement.GetString()),
            _ => throw new JsonException($"NUMBERに対して予期しないValueKindです: {valueElement.ValueKind}")
        };
    }
    private static int? ParseIntFromString(string? str) {
        if (string.IsNullOrWhiteSpace(str)) { return null; }
        if (int.TryParse(str, out var result)) { return result; }
        throw new JsonException($"文字列 '{str}' を整数に変換できませんでした。");
    }

}
