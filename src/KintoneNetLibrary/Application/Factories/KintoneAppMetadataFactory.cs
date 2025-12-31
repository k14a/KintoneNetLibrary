using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Factories;

public static class KintoneAppMetadataFactory {
    public static KintoneAppMetadata Create(string fieldsJson, string layoutJson, int appId) {
        var fields = ParseFields(fieldsJson);

        // layoutJson は最初は使わなくてもOK
        return new KintoneAppMetadata { AppId = appId, Fields = fields };
    }

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
                Code = code,
                Label = label,
                Type = KintoneFieldType.SubTable,
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
            Code = code,
            Label = label,
            Type = ParseFieldType(type),
            Required = required,
            Options = options
        };
    }

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
