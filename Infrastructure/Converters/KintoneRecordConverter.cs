using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

public class KintoneRecordConverter<T> : JsonConverter<T> where T : KintoneModelBase, new() {
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var model = new T();
        var props = typeof(T).GetProperties();

        foreach (var prop in props) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) {
                continue;
            }

            var fieldCode = attr.FieldCode;
            if (!root.TryGetProperty(fieldCode, out var fieldElement)) {
                continue;
            }

            if (!fieldElement.TryGetProperty("type", out var typeElement) ||
                !fieldElement.TryGetProperty("value", out var valueElement)) {
                continue;
            }

            var kintoneType = typeElement.GetString();
            if (string.IsNullOrWhiteSpace(kintoneType)) {
                continue;
            }

            try {
                var value = KintoneValueConverter.ConvertToCSharp(valueElement, attr.FieldType, prop.PropertyType);
                prop.SetValue(model, value);
            } catch (Exception ex) {
                throw new JsonException($"プロパティ '{prop.Name}' (FieldCode='{fieldCode}') の変換に失敗しました。KintoneType='{kintoneType}', TargetType='{prop.PropertyType.Name}'", ex);
            }
        }

        return model;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        throw new NotImplementedException("書き込みはまだ未実装です");
    }
}

public class KintoneRecordConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        return typeof(KintoneModelBase).IsAssignableFrom(typeToConvert);
    }
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var converterType = typeof(KintoneRecordConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
