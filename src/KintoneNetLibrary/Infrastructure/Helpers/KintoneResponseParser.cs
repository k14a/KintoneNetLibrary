using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public static class KintoneResponseParser {
    private sealed class DeleteResponse {
        [JsonPropertyName("ids")]
        public List<string> IDs { get; set; } = [];
    }

    /// <summary>
    /// CreateRecordsAsync などのレスポンス JSON を元に、作成結果を元のモデルに反映する
    /// </summary>
    public static IList<T> ParseCreatedRecords<T>(IList<T> originalRecords, string responseJson) where T : KintoneModelBase<T>, new() {
        var indexes = KintoneRecordIndexesResponse.Parse(responseJson).ToIndexes();

        // var originals = originalRecords.ToList();
        if (indexes.IDs.Count != originalRecords.Count) {
            throw new KintoneException("Mismatch between the number of request and response records.");
        }

        for (int i = 0; i < originalRecords.Count; i++) {
            originalRecords[i].ID = indexes.IDs[i] ?? string.Empty;
            if (int.TryParse(indexes.Revisions[i], out var rev)) {
                originalRecords[i].Revision = rev;
            }
        }

        return originalRecords;
    }
    public static T ParseRecord<T>(string json) where T : KintoneModelBase<T>, new() {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("record", out var recordElement)) {
            throw new InvalidOperationException("Missing 'record' property in JSON.");
        }

        var modelJson = recordElement.GetRawText();
        return JsonSerializer.Deserialize<T>(modelJson, KintoneJsonOptions.Default)!;

    }
    public static IList<T> ParseRecords<T>(string json) where T : KintoneModelBase<T>, new() {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("records", out var recordsElement)) {
            throw new InvalidOperationException("Missing 'records' property in JSON.");
        }

        var result = new List<T>();

        foreach (var recordElement in recordsElement.EnumerateArray()) {
            var dict = new Dictionary<string, JsonElement>();

            foreach (var prop in recordElement.EnumerateObject()) {
                dict[prop.Name] = prop.Value;
            }

            var model = new T();
            model.LoadFromJsonDictionary(dict);
            result.Add(model);
        }

        return result;
    }
}
