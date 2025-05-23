using System.Text.Json.Serialization;
using Microsoft.VisualBasic;

namespace KintoneNetLibrary.Types;

/// <summary>
/// 単一レコードの ID / Revision を表す DTO.
/// </summary>
public class KintoneIndex {
    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;
    [JsonPropertyName("revision")]
    public string RevisionString { get; set; } = "-1";
    [JsonIgnore()]
    public int Revision {
        get => int.TryParse(this.RevisionString, out var revision) ? revision : -1;
        set => this.RevisionString = value.ToString();
    }
}
