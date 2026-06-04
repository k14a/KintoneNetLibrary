using System.Text.Json;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのフィールド値をC#の型に変換するためのユーティリティクラス
/// </summary>
public static class KintoneValueConverter {
    /// <summary>
    /// Kintoneのフィールド値をC#の型に変換します。
    /// </summary>
    /// <param name="valueElement">Kintoneのフィールド値を表すJsonElement</param>
    /// <param name="fieldType">Kintoneのフィールドタイプ</param>
    /// <param name="targetType">変換先のC#の型</param>
    /// <returns>変換されたC#のオブジェクト</returns>
    /// <exception cref="NotSupportedException">サポートされていない変換が要求された場合にスローされます</exception>
    public static object? ConvertToCSharp(JsonElement valueElement, KintoneFieldType fieldType, Type targetType, System.Text.Json.JsonSerializerOptions? jsonOptions = null) {
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
                (Type t, KintoneFieldType.Date) when t == typeof(KintoneDateOnly) => new KintoneDateOnly(str),
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

        if (fieldType == KintoneFieldType.SubTable && valueElement.ValueKind == JsonValueKind.Array) {
            if (valueElement.GetArrayLength() == 0) { return Activator.CreateInstance(targetType); }
            Type elementType = targetType.GetGenericArguments()[0];

            var list = new List<object>();

            foreach (var row in valueElement.EnumerateArray()) {
                var raw = row.GetProperty("value").GetRawText();

                var detail = JsonSerializer.Deserialize(raw, elementType, jsonOptions);
                if (detail != null) {
                    list.Add(detail);
                }
            }

            return list;
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
            (Type t, JsonValueKind.Object) when t == typeof(KintoneGroup) => JsonSerializer.Deserialize<KintoneGroup>(valueElement.GetRawText()),
            (Type t, JsonValueKind.Object) when t == typeof(KintoneOrganization) => JsonSerializer.Deserialize<KintoneOrganization>(valueElement.GetRawText()),
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
            (Type t, JsonValueKind.Array) when typeof(IEnumerable<KintoneGroup>).IsAssignableFrom(t) =>
                valueElement.EnumerateArray()
                    .Select(u => new KintoneGroup {
                        Code = u.GetProperty("code").GetString() ?? "",
                        Name = u.GetProperty("name").GetString() ?? ""
                    }).ToList(),
            (Type t, JsonValueKind.Array) when typeof(IEnumerable<KintoneOrganization>).IsAssignableFrom(t) =>
                valueElement.EnumerateArray()
                    .Select(u => new KintoneOrganization {
                        Code = u.GetProperty("code").GetString() ?? "",
                        Name = u.GetProperty("name").GetString() ?? ""
                    }).ToList(),
            _ => throw new NotSupportedException($"Unsupported value conversion to {targetType.Name} from kind: {valueElement.ValueKind}")
        };
    }

    /// <summary>
    /// 文字列からNullableな整数をパースします。
    /// </summary>
    /// <param name="str">パースする文字列</param>
    /// <returns>パースされたNullableな整数</returns>
    /// <exception cref="JsonException">文字列が整数に変換できない場合にスローされます</exception>
    private static int? ParseIntFromString(string? str) {
        if (string.IsNullOrWhiteSpace(str)) { return null; }
        if (int.TryParse(str, out var result)) { return result; }
        throw new JsonException($"文字列 '{str}' を整数に変換できませんでした。");
    }
}
