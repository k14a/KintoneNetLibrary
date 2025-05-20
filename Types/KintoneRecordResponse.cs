using System.Text.Json.Serialization;
using KintoneNetLibrary.Model;

namespace KintoneNetLibrary.Types;

public class KintoneRecordResponse<T> where T : KintoneModelBase, new() {
    [JsonPropertyName("record")]
    public T Record { get; set; } = default!;
    [JsonPropertyName("revision")]
    public string? RevisionRaw { get; set; }
    [JsonIgnore()]
    public int? Revision => int.TryParse(this.RevisionRaw, out var revision) ? revision : null;
}