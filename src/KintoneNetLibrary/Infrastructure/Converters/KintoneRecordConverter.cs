using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

/// <summary>
/// KintoneのレコードをC#のモデルに変換するためのJsonConverter
/// </summary>
/// <typeparam name="T">変換するモデルの型</typeparam>
public class KintoneRecordConverter<T> : JsonConverter<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// KintoneのレコードJSONをC#のモデルに変換します
    /// </summary>
    /// <param name="reader">JSONリーダー</param>
    /// <param name="typeToConvert">変換する型</param>
    /// <param name="options">JsonSerializerのオプション</param>
    /// <returns>変換されたモデル</returns>
    /// <exception cref="JsonException">変換に失敗した場合にスローされます</exception>
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
            if (!root.TryGetProperty(fieldCode, out JsonElement fieldElement)) {
                var fallbackCodes = model.ConvertDictionary
                    .Where(c => c.PropertyName == prop.Name && c.ConvertDirection == NameConverter.Direction.Read)
                    .Select(c => c.ItemName);
                var found = false;
                foreach (var fallbackCode in fallbackCodes) {
                    if (root.TryGetProperty(fallbackCode, out fieldElement)) {
                        fieldCode = fallbackCode;
                        found = true;
                        break;
                    }
                }
                if (!found) { continue; }
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
                var value = KintoneValueConverter.ConvertToCSharp(valueElement, attr.FieldType, prop.PropertyType, KintoneJsonOptions.Default);
                prop.SetValue(model, value);
            } catch (Exception ex) {
                throw new JsonException($"プロパティ '{prop.Name}' (FieldCode='{fieldCode}') の変換に失敗しました。KintoneType='{kintoneType}', TargetType='{prop.PropertyType.Name}'", ex);
            }
        }

        return model;
    }

    /// <summary>
    /// C#のモデルをKintoneのレコードJSONに変換します（未実装）
    /// </summary>
    /// <param name="writer">JSONライター</param>
    /// <param name="value">変換するモデル</param>
    /// <param name="options">JsonSerializerのオプション</param>
    /// <exception cref="NotImplementedException"></exception>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        throw new NotImplementedException("書き込みはまだ未実装です");
    }
}

/// <summary>
/// KintoneRecordConverterのファクトリクラス
/// </summary>
/// <typeparam name="T">変換するモデルの型</typeparam>
public class KintoneRecordConverterFactory<T> : JsonConverterFactory where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// 指定された型が変換可能かどうかを判定します
    /// </summary>
    /// <param name="typeToConvert">判定する型</param>
    /// <returns>変換可能な場合はtrue、それ以外の場合はfalse</returns>
    public override bool CanConvert(Type typeToConvert) {
        return typeof(T).IsAssignableFrom(typeToConvert);
    }
    /// <summary>
    /// 指定された型に対するJsonConverterを作成します
    /// </summary>
    /// <param name="typeToConvert">作成する型</param>
    /// <param name="options">JsonSerializerのオプション</param>
    /// <returns>作成されたJsonConverter</returns>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var converterType = typeof(KintoneRecordConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
