using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneField
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;

    public KintoneField() { }

    public KintoneField(string type, object value) {
        this.Type = type;
        this.Value = value;
    }
}