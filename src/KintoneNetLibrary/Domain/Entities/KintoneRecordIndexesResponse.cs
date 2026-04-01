using System.Text.Json;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

internal class KintoneRecordIndexesResponse {
    [JsonPropertyName("records")]
    public List<KintoneRecordIndexItem> Records { get; set; } = [];

    public static KintoneRecordIndexesResponse Parse(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("JSON string is null or empty", nameof(json));
        }

        return JsonSerializer.Deserialize<KintoneRecordIndexesResponse>(json) ?? new KintoneRecordIndexesResponse();
    }

    public KintoneIndexes ToIndexes() {
        var ids = new List<string?>();
        var revisions = new List<string?>();

        foreach (var record in this.Records) {
            if (!string.IsNullOrEmpty(record.ID)) {
                ids.Add(record.ID);
            }
            if (!string.IsNullOrEmpty(record.RevisionString)) {
                revisions.Add(record.RevisionString);
            }
        }
        return new KintoneIndexes {
            IDs = ids,
            Revisions = revisions
        };
    }
}

internal class KintoneRecordIndexItem {
    [JsonPropertyName("id")]
    public string? ID { get; set; }
    [JsonPropertyName("revision")]
    public string? RevisionString { get; set; }
    public int Revision => int.TryParse(this.RevisionString, out var rev) ? rev : -1;
}
