using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneRecords<T>
{
    [JsonPropertyName("app")]
    public int AppID { get; set; }

    [JsonPropertyName("ids")]
    public List<string>? IDs { get; set; }

    [JsonPropertyName("revisions")]
    public List<int>? Revisions { get; set; }

    [JsonPropertyName("records")]
    public List<T> Records { get; set; } = new();
}