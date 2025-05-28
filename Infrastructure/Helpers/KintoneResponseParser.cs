using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public static class KintoneResponseParser {
    private sealed class DeleteResponse {
        [JsonPropertyName("ids")]
        public List<string> IDs { get; set; } = [];
    }

    /// <summary>
    /// CreateRecordsAsync などのレスポンス JSON を元に、作成結果を元のモデルに反映する
    /// </summary>
    public static IList<T> ParseCreatedRecords<T>(IEnumerable<T> originalRecords, string responseJson) where T : KintoneModelBase, new() {
        var indexes = KintoneRecordIndexesResponse.Parse(responseJson).ToIndexes();

        var originals = originalRecords.ToList();
        if (indexes.IDs.Count != originals.Count) {
            throw new KintoneException("Mismatch between the number of request and response records.");
        }

        for (int i = 0; i < originals.Count; i++) {
            originals[i].ID = indexes.IDs[i] ?? string.Empty;
            if (int.TryParse(indexes.Revisions[i], out var rev)) {
                originals[i].Revision = rev;
            }
        }

        return originals;
    }
    public static T ParseRecord<T>(string json) where T : KintoneModelBase, new() {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("record", out var recordElement)) {
            throw new InvalidOperationException("Missing 'record' property in JSON.");
        }

        var model = new T();
        var dict = new Dictionary<string, JsonElement>();

        foreach (var prop in recordElement.EnumerateObject()) {
            if (prop.Value.TryGetProperty("value", out var valueElement)) {
                dict[prop.Name] = valueElement;
            }
        }

        model.LoadFromJsonDictionary(dict);
        return model;
    }
    public static IList<T> ParseRecords<T>(string json) where T : KintoneModelBase, new() {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("records", out var recordsElement)) {
            throw new InvalidOperationException("Missing 'records' property in JSON.");
        }

        var result = new List<T>();

        foreach (var recordElement in recordsElement.EnumerateArray()) {
            var dict = new Dictionary<string, JsonElement>();

            foreach (var prop in recordElement.EnumerateObject()) {
                if (prop.Value.TryGetProperty("value", out var valueElement)) {
                    dict[prop.Name] = valueElement;
                }
            }

            var model = new T();
            model.LoadFromJsonDictionary(dict);
            result.Add(model);
        }

        return result;
    }
    /// <summary>
    /// Kintoneの削除レスポンスJSONから削除されたレコードIDのリストを取得する
    /// </summary>
    /// <param name="json">Kintoneの削除APIからのレスポンスJSON</param>
    /// <returns>削除されたレコードIDのリスト</returns>
    [Obsolete()]
    public static List<string> ParseDeletedRecords(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("JSON string is null or empty", nameof(json));
        }

        var response = JsonSerializer.Deserialize<DeleteResponse>(json)
            ?? throw new InvalidOperationException("Failed to parse deleted records response JSON");

        return response.IDs;
    }

}
