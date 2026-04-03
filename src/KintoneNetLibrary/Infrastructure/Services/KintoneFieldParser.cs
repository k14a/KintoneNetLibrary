using System.Text.Json;
using KintoneNetLibrary.Domain.Converters;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Services;

/// <summary>
/// KintoneフィールドのメタデータをJSONから解析するための実装クラス
/// </summary>
public class KintoneFieldParser : IKintoneFieldParser {
    /// <summary>
    /// KintoneフィールドのJSON要素を解析して、フィールドのメタデータを取得します。
    /// </summary>
    /// <param name="properties">フィールドのJSON要素</param>
    /// <returns>解析されたフィールドのメタデータのリスト</returns>
    public List<KintoneFieldMetadata> Parse(JsonElement properties) {
        var fields = new List<KintoneFieldMetadata>();

        foreach (var prop in properties.EnumerateObject()) {
            var code = prop.Name;
            var field = prop.Value;

            var type = field.GetProperty("type").GetString()!;
            var label = field.GetProperty("label").GetString() ?? code;

            if (type == "SUBTABLE") {
                var subFieldsJson = field.GetProperty("fields");
                var subFields = new List<KintoneFieldMetadata>();

                foreach (var sf in subFieldsJson.EnumerateObject()) {
                    var sfCode = sf.Name;
                    var sfValue = sf.Value;
                    var sfType = sfValue.GetProperty("type").GetString()!;

                    if (!KintoneFieldTypeMapper.TryConvert(sfType, out var fieldType)) {
                        continue;
                    }

                    subFields.Add(new KintoneFieldMetadata {
                        FieldCode = sfCode,
                        FieldLabel = sfValue.GetProperty("label").GetString() ?? sfCode,
                        FieldType = fieldType,
                        FieldTypeName = fieldType.ToString(),
                        OriginalFieldType = sfType,
                        Required = sfValue.TryGetProperty("required", out var req) && req.GetBoolean(),
                        Options = ExtractOptions(sfValue)
                    });
                }

                fields.Add(new KintoneFieldMetadata {
                    FieldCode = code,
                    FieldLabel = label,
                    FieldType = KintoneFieldType.SubTable,
                    FieldTypeName = KintoneFieldType.SubTable.ToString(),
                    OriginalFieldType = type,
                    Required = false,
                    SubFields = subFields
                });
            } else {
                if (!KintoneFieldTypeMapper.TryConvert(type, out var fieldType)) {
                    continue;
                }

                fields.Add(new KintoneFieldMetadata {
                    FieldCode = code,
                    FieldLabel = label,
                    FieldType = fieldType,
                    FieldTypeName = fieldType.ToString(),
                    OriginalFieldType = type,
                    Required = field.TryGetProperty("required", out var req) && req.GetBoolean(),
                    Options = ExtractOptions(field)
                });
            }
        }

        return fields;
    }

    /// <summary>
    /// フィールドのJSON要素から選択肢のリストを抽出します。
    /// 選択肢が存在しない場合は空のリストを返します。
    /// 解析対象のフィールドタイプは、ドロップダウン、ラジオボタン、チェックボックスなどです。
    /// </summary>
    /// <param name="field">フィールドのJSON要素</param>
    /// <returns>選択肢のリスト</returns>
    private static List<string> ExtractOptions(JsonElement field) {
        if (!field.TryGetProperty("options", out var optionsJson)) {
            return [];
        }

        return optionsJson.EnumerateObject()
            .Select(o => o.Name)
            .ToList();
    }
}
