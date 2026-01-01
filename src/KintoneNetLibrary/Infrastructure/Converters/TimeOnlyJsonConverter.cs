using System.Text.Json;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Infrastructure.Converters;

// コメントは日本語で記述
/// <summary>
/// TimeOnly型のJSONコンバーター
/// </summary>
public class TimeOnlyJsonConverter : JsonConverter<TimeOnly> {
    private const string Format = "HH:mm";

    /// <summary>
    /// JSONからTimeOnly型への変換
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TimeOnly.ParseExact(reader.GetString()!, Format);

    /// <summary>
    /// TimeOnly型からJSONへの変換
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format));
}

