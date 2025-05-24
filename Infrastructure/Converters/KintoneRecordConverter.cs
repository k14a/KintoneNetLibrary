using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

public class KintoneRecordConverter<T> : JsonConverter<T> where T : KintoneModelBase, new() {
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var jsonDoc = JsonDocument.ParseValue(ref reader);
        // var recordRoot = jsonDoc.RootElement.GetProperty("record");
        var root = jsonDoc.RootElement;

        var model = new T();
        var props = typeof(T).GetProperties();

        foreach (var prop in props) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) { continue; }

            var fieldName = attr.FieldCode;
            if (!root.TryGetProperty(fieldName, out var fieldElement)) { continue; }

            var type = fieldElement.GetProperty("type").GetString();
            var valueElement = fieldElement.GetProperty("value");

            var value = KintoneValueConverter.ConvertToCSharp(prop.PropertyType, type!, valueElement);
            prop.SetValue(model, value);
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
