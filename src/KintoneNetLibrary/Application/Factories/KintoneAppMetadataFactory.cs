using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Application.Factories;

/// <summary>
/// Kintoneアプリのメタデータを生成するファクトリクラス
/// </summary>
public static class KintoneAppMetadataFactory {
    /// <summary>
    /// Kintoneアプリのメタデータを生成する
    /// </summary>
    /// <param name="fieldsJson">フィールド情報のJSON文字列</param>
    /// <param name="layoutJson">レイアウト情報のJSON文字列</param>
    /// <param name="appId">アプリID</param>
    /// <returns>生成されたKintoneアプリのメタデータ</returns>
    public static KintoneAppMetadata Create(string fieldsJson, string layoutJson, int appId) {
        var fields = ParseFields(fieldsJson);

        // layoutJson は最初は使わなくてもOK
        return new KintoneAppMetadata { AppId = appId, Fields = fields };
    }

    /// <summary>
    /// フィールド情報をパースする
    /// </summary>
    /// <param name="fieldsJson">フィールド情報のJSON文字列</param>
    /// <returns>パースされたフィールド情報のリスト</returns>
    private static List<KintoneFieldMetadata> ParseFields(string fieldsJson) {
        using var doc = JsonDocument.Parse(fieldsJson);
        var root = doc.RootElement;
        var properties = root.GetProperty("properties");
        var result = new List<KintoneFieldMetadata>();

        foreach (var prop in properties.EnumerateObject()) {
            var field = ParseField(prop.Value);
            result.Add(field);
        }

        return result;
    }

    /// <summary>
    /// フィールド情報をパースする
    /// </summary>
    /// <param name="element">フィールドのJSON要素</param>
    /// <returns>パースされたフィールド情報</returns>
    private static KintoneFieldMetadata ParseField(JsonElement element) {
        var type = element.GetProperty("type").GetString()!;
        var code = element.GetProperty("code").GetString()!;
        var label = element.GetProperty("label").GetString()!;
        var required = element.TryGetProperty("required", out var req) && req.GetBoolean();

        // サブテーブルの場合
        if (type == "SUBTABLE") {
            var fields = new List<KintoneFieldMetadata>();
            var subFields = element.GetProperty("fields");

            foreach (var sub in subFields.EnumerateObject()) {
                fields.Add(ParseField(sub.Value));
            }

            return new KintoneFieldMetadata {
                FieldCode = code,
                FieldLabel = label,
                FieldType = KintoneFieldType.SubTable,
                Required = required,
                SubFields = fields
            };
        }

        // 選択肢系
        List<string>? options = null;
        if (element.TryGetProperty("options", out var opt)) {
            options = opt.EnumerateObject().Select(o => o.Name).ToList();
        }

        return new KintoneFieldMetadata {
            FieldCode = code,
            FieldLabel = label,
            FieldType = ParseFieldType(type),
            Required = required,
            Options = options
        };
    }

    /// <summary>
    /// フィールドタイプをパースする
    /// </summary>
    /// <param name="type">フィールドタイプの文字列</param>
    /// <returns>パースされたフィールドタイプ</returns>
    private static KintoneFieldType ParseFieldType(string type) {
        return type switch {
            "SINGLE_LINE_TEXT" => KintoneFieldType.SingleLineText,
            "NUMBER" => KintoneFieldType.Number,
            "DATE" => KintoneFieldType.Date,
            "DATETIME" => KintoneFieldType.DateTime,
            "CHECK_BOX" => KintoneFieldType.CheckBox,
            "RADIO_BUTTON" => KintoneFieldType.RadioButton,
            "DROP_DOWN" => KintoneFieldType.DropDown,
            "SUBTABLE" => KintoneFieldType.SubTable,
            _ => KintoneFieldType.Unknown
        };
    }
}
