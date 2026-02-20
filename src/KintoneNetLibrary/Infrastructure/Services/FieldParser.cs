using System.Text.Json;
using KintoneNetLibrary.Domain.Converters;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Services;

public class FieldParser : IFieldParser {
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

    private List<string> ExtractOptions(JsonElement field) {
        if (!field.TryGetProperty("options", out var optionsJson)) {
            return [];
        }

        return optionsJson.EnumerateObject()
            .Select(o => o.Name)
            .ToList();
    }
}
