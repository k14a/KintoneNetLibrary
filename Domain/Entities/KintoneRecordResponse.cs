using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

public class KintoneRecordResponse<T> where T : KintoneModelBase<T>, new() {
    [JsonPropertyName("record")]
    public T Record { get; set; } = default!;
    [JsonPropertyName("revision")]
    public string? RevisionRaw { get; set; }
    [JsonIgnore()]
    public int? Revision => int.TryParse(this.RevisionRaw, out var revision) ? revision : null;
}