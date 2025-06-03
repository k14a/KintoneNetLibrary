using System.Text.Json;
using System.Text.Json.Nodes;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

internal static class KintoneValueConverter {
    public static object? ConvertToCSharp(JsonElement valueElement, KintoneDateTimeType dateTimeType, Type targetType) {
        if (valueElement.ValueKind == JsonValueKind.Null) {
            return null;
        }

        bool IsStringEnum(Type t) => t == typeof(IEnumerable<string>) || t == typeof(List<string>);

        // --- 日付・時間型の優先処理 ---
        if (dateTimeType != KintoneDateTimeType.Unknown) {
            var str = valueElement.GetString();
            return (targetType, dateTimeType) switch {
                (Type t, KintoneDateTimeType.DateTime) when t == typeof(DateTime) => DateTime.TryParse(str, out var dt) ? dt : DateTime.MinValue,
                (Type t, KintoneDateTimeType.DateTime) when t == typeof(DateTime?) => DateTime.TryParse(str, out var dt) ? dt : null,
                (Type t, KintoneDateTimeType.TimeOnly) when t == typeof(TimeOnly) => TimeOnly.TryParse(str, out var to) ? to : TimeOnly.MinValue,
                (Type t, KintoneDateTimeType.TimeOnly) when t == typeof(TimeOnly?) => TimeOnly.TryParse(str, out var to) ? to : null,
                (Type t, KintoneDateTimeType.DateOnly) when t == typeof(DateOnly) => DateOnly.TryParse(str, out var d) ? d : DateOnly.MinValue,
                (Type t, KintoneDateTimeType.DateOnly) when t == typeof(DateOnly?) => DateOnly.TryParse(str, out var d) ? d : null,
                _ => throw new NotSupportedException($"Unsupported KintoneDateTimeType '{dateTimeType}' to {targetType.Name}")
            };
        }

        // --- 通常のKintoneタイプ判定（string） ---
        var kintoneType = valueElement.ValueKind switch {
            JsonValueKind.String => "STRING",
            JsonValueKind.Number => "NUMBER",
            JsonValueKind.Array => "ARRAY",
            JsonValueKind.Object => "OBJECT",
            _ => "UNKNOWN"
        };

        // 下記は従来の string ベースでの判定（現状のまま維持）
        return (targetType, kintoneType) switch {
            (Type t, "STRING") when t == typeof(string) => valueElement.GetString(),
            (Type t, "NUMBER") when t == typeof(int) => ParseNullableInt(valueElement) ?? 0,
            (Type t, "NUMBER") when t == typeof(int?) => ParseNullableInt(valueElement),
            (Type t, "OBJECT") when t == typeof(KintoneUser) => JsonSerializer.Deserialize<KintoneUser>(valueElement.GetRawText()),
            (Type t, "STRING") when t == typeof(string) => valueElement.GetString(),
            (Type t, "STRING") when t == typeof(int) => int.TryParse(valueElement.GetString(), out var i) ? i : 0,
            (Type t, "STRING") when t == typeof(int?) => int.TryParse(valueElement.GetString(), out var i) ? i : null,
            (Type t, "ARRAY") when IsStringEnum(t) => valueElement.EnumerateArray().Select(e => e.GetString()!).ToList(),
            (Type t, "ARRAY") when t == typeof(List<KintoneFile>)
                => valueElement.EnumerateArray()
                    .Select(f => new KintoneFile {
                        ContentType = f.GetProperty("contentType").GetString() ?? "",
                        FileKey = f.GetProperty("fileKey").GetString() ?? "",
                        Name = f.GetProperty("name").GetString() ?? "",
                        Size = long.TryParse(f.GetProperty("size").GetString(), out var size) ? size : 0
                    }).ToList(),
            (Type t, "ARRAY") when t == typeof(List<Dictionary<string, JsonElement>>)
                => valueElement.EnumerateArray()
                    .Select(row => row.GetProperty("value")
                        .EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value))
                    .ToList(),

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
