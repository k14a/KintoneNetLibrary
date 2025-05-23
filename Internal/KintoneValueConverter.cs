using System.Text.Json;
using System.Text.Json.Nodes;
using KintoneNetLibrary.Types;

namespace KintoneNetLibrary.Internal;

internal static class KintoneValueConverter {
    public static object? Convert(JsonNode fieldNode, string type) {
        var valueNode = fieldNode["value"];
        if (valueNode is null || valueNode.ToJsonString() == "null") {
            return null;
        }

        return type switch {
            "SINGLE_LINE_TEXT" or
            "MULTI_LINE_TEXT" or
            "RICH_TEXT" or
            "DROP_DOWN" or
            "RADIO_BUTTON" or
            "LINK" or
            "STATUS" or
            "STATUS_ASSIGNEE" or
            "CALC" or
            "TEXT" or
            "CATEGORY" or
            "CREATOR" or
            "MODIFIER" or
            "__REVISION__" or
            "__ID__" or
            "RECORD_NUMBER" => valueNode.GetValue<string>(),

            "NUMBER" => int.TryParse(valueNode.ToString(), out var i) ? i : double.Parse(valueNode.ToString()),

            "CHECK_BOX" or
            "MULTI_SELECT" or
            "USER_SELECT" or
            "GROUP_SELECT" or
            "ORGANIZATION_SELECT" =>
                valueNode.AsArray().Select(x => x["code"]?.GetValue<string>() ?? string.Empty).ToList(),

            "DATE" or
            "UPDATED_TIME" or
            "CREATED_TIME" => DateTime.Parse(valueNode.ToString()),

            "TIME" => ConvertKintoneTime(valueNode.ToString()),

            "DATETIME" => DateTime.Parse(valueNode.ToString()),

            "FILE" => valueNode.AsArray().Select(f => new KintoneFile {
                ContentType = f["contentType"]?.GetValue<string>() ?? "",
                FileKey = f["fileKey"]?.GetValue<string>() ?? "",
                Name = f["name"]?.GetValue<string>() ?? "",
                Size = long.TryParse(f["size"]?.ToString(), out var size) ? size : 0
            }).ToList(),

            "USER" => new KintoneUser {
                Code = valueNode["code"]?.GetValue<string>() ?? "",
                Name = valueNode["name"]?.GetValue<string>() ?? ""
            },

            _ => valueNode.ToJsonString()
        };
    }

    public static object? ConvertToCSharp(Type targetType, string kintoneType, JsonElement valueElement) {
        if (valueElement.ValueKind == JsonValueKind.Null) {
            return null;
        }

        return (targetType, kintoneType) switch {
            (Type t, "SINGLE_LINE_TEXT" or "MULTI_LINE_TEXT") when t == typeof(string) => valueElement.GetString(),
            (Type t, "NUMBER") when t == typeof(int) => ParseNullableInt(valueElement) ?? 0,
            (Type t, "NUMBER") when t == typeof(int?) => ParseNullableInt(valueElement),
            (Type t, "DATETIME") when t == typeof(DateTime) => DateTime.TryParse(valueElement.GetString(), out var dt) ? dt : DateTime.MinValue,
            (Type t, "TIME") when t == typeof(DateTime) => TimeSpan.TryParse(valueElement.GetString(), out var ts) ? DateTime.MinValue.Add(ts) : DateTime.MinValue,
            (Type t, "CREATOR" or "MODIFIER") when t == typeof(KintoneUser) => JsonSerializer.Deserialize<KintoneUser>(valueElement.GetRawText()),
            _ => throw new NotSupportedException($"Unsupported Kintone type '{kintoneType}' to {targetType.Name}")
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

    // public static object? ConvertToCSharp(Type targetType, string kintoneType, JsonElement valueElement) {
    //     if (valueElement.ValueKind == JsonValueKind.Null) {
    //         return null;
    //     }

    //     return (targetType, kintoneType) switch {
    //         (Type t, "SINGLE_LINE_TEXT" or "MULTI_LINE_TEXT") when t == typeof(string) =>
    //             valueElement.GetString(),

    //         (Type t, "NUMBER") when t == typeof(int) =>
    //             valueElement.ValueKind switch {
    //                 JsonValueKind.Number => valueElement.GetInt32(),
    //                 JsonValueKind.String => ((Func<int>)(() => { // または Func<int?> など、期待される型に応じて
    //                     var str = valueElement.GetString();
    //                     if (string.IsNullOrWhiteSpace(str)) {
    //                         // このreturnはラムダ式からのreturnとなり、
    //                         // switch式のアームの値として使われます。
    //                         // もしnullを返したい場合で、アームの型が int? なら return null;
    //                         return 0;
    //                     }
    //                     // 安全のため TryParse を使用することを推奨します
    //                     if (int.TryParse(str, out var parsedInt)) {
    //                         return parsedInt;
    //                     }
    //                     // パースに失敗した場合の処理 (例: デフォルト値を返すか、例外を投げる)
    //                     // ここでは例として例外をスローします
    //                     throw new JsonException($"文字列 '{str}' を整数に変換できませんでした。");
    //                     // return 0; // もしくは適切なデフォルト値
    //                 }))(), // ラムダ式を定義し、即座に実行する ()
    //                 _ => throw new JsonException($"NUMBERに対して予期しないValueKindです: {valueElement.ValueKind}")
    //             },

    //         (Type t, "DATETIME") when t == typeof(DateTime) =>
    //             DateTime.TryParse(valueElement.GetString(), out var dt) ? dt : DateTime.MinValue,

    //         (Type t, "TIME") when t == typeof(DateTime) =>
    //             TimeSpan.TryParse(valueElement.GetString(), out var ts) ? DateTime.MinValue.Add(ts) : DateTime.MinValue,

    //         (Type t, "CREATOR" or "MODIFIER") when t == typeof(KintoneUser) =>
    //             JsonSerializer.Deserialize<KintoneUser>(valueElement.GetRawText()),

    //         _ => throw new NotSupportedException($"Unsupported Kintone type '{kintoneType}' to {targetType.Name}")
    //     };
    // }

    private static DateTime ConvertKintoneTime(string value) {
        // KintoneのTIMEは "HH:mm" 形式、日付は無意味なので MinValue に設定
        if (TimeSpan.TryParse(value, out var time)) {
            return DateTime.MinValue.Date + time;
        }

        throw new FormatException($"Invalid TIME format: {value}");
    }
}
