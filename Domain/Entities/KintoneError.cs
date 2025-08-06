using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneError {
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
    public bool HasDetails => this.Details is { Count: > 0 };

    public KintoneError() { }
    public KintoneError(string message) { this.Message = message; }

    public override string ToString() {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"[KintoneError] Code: {this.Code}, Message: {this.Message}");
        if (!string.IsNullOrEmpty(this.Summary)) { builder.AppendLine($"Summary: {this.Summary}"); }
        if (!string.IsNullOrEmpty(this.ID)) { builder.AppendLine($"ID: {this.ID}"); }
        if (this.HasDetails) {
            builder.AppendLine("Details:");
            foreach (var detail in this.Details) {
                builder.AppendLine($"  - {detail}");
            }
        }
        if (this.Errors != null) { builder.AppendLine($"Errors: {this.Errors}"); }

        return builder.ToString();
    }
    public string ToJson(bool indented = false) {
        var options = JsonOptionsUtil.Clone(JsonSerializerOptions.Default, indented);
        return JsonSerializer.Serialize(this, options);
    }

}

public class KintoneErrorDetail {
    [JsonPropertyName("index")]
    public int Index { get; set; }
    [JsonPropertyName("messages")]
    public IList<string> Messages { get; set; } = [];

    public override string ToString() {
        return $"Index {this.Index}: {string.Join(", ", this.Messages)}";
    }
}
