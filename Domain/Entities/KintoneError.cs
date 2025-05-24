using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
    [JsonPropertyName("details")]
    public IList<KintoneErrorDetail> Details { get; set; } = [];
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    [JsonIgnore]
    public bool HasDetails => Details is { Count: > 0 };

    public KintoneError() { }
    public KintoneError(string message) { this.Message = message; }

    public override string ToString()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"[KintoneError] Code: {Code}, Message: {Message}");
        if (!string.IsNullOrEmpty(Summary)) {
            builder.AppendLine($"Summary: {Summary}");
        }
        if (!string.IsNullOrEmpty(ID)) {
            builder.AppendLine($"ID: {ID}");
        }
        if (HasDetails) {
            builder.AppendLine("Details:");
            foreach (var detail in Details) {
                builder.AppendLine($"  - {detail}");
            }
        }
        if (Errors != null) {
            builder.AppendLine($"Errors: {Errors}");
        }

        return builder.ToString();
    }
}

public class KintoneErrorDetail
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    [JsonPropertyName("messages")]
    public IList<string> Messages { get; set; } = [];

    public override string ToString()
    {
        return $"Index {Index}: {string.Join(", ", Messages)}";
    }
}
