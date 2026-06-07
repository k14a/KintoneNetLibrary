using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Infrastructure.Helpers;

/// <summary>
/// Kintone のレスポンスを解析するためのヘルパークラス
/// </summary>
public static class KintoneResponseParser {
    /// <summary>
    /// DeleteRecordsAsync などのレスポンス JSON を解析するための内部クラス
    /// </summary>
    private sealed class DeleteResponse {
        /// <summary>
        /// 削除されたレコードの Id のリスト
        /// </summary>
        [JsonPropertyName("ids")]
        public List<string> Ids { get; set; } = [];
    }

    /// <summary>
    /// CreateRecordsAsync のレスポンス JSON を解析して、元のレコードリストに Id と Revision をセットするためのメソッド
    /// </summary>
    /// <typeparam name="T">解析対象のモデルの型</typeparam>
    /// <param name="originalRecords">元のレコードリスト</param>
    /// <param name="responseJson">レスポンス JSON</param>
    /// <returns>Id と Revision がセットされたレコードリスト</returns>
    /// <exception cref="KintoneException">レスポンスのレコード数が一致しない場合にスローされます</exception>
    public static IList<T> ParseCreatedRecords<T>(IList<T> originalRecords, string responseJson) where T : KintoneModelBase<T>, new() {
        var indexes = KintoneIndexes.Parse(responseJson);

        // var originals = originalRecords.ToList();
        if (indexes.Ids.Count != originalRecords.Count) {
            throw new KintoneException("Mismatch between the number of request and response records.");
        }

        for (int i = 0; i < originalRecords.Count; i++) {
            originalRecords[i].Id = indexes.Ids[i] ?? string.Empty;
            if (int.TryParse(indexes.Revisions[i], out var rev)) {
                originalRecords[i].Revision = rev;
            }
        }

        return originalRecords;
    }

    /// <summary>
    /// 単一レコードの JSON を解析してモデルに変換する
    /// </summary>
    /// <typeparam name="T">解析対象のモデルの型</typeparam>
    /// <param name="json">解析対象の JSON 文字列</param>
    /// <returns>解析結果のモデル</returns>
    /// <exception cref="InvalidOperationException">JSON に 'record' プロパティが存在しない場合にスローされます</exception>
    public static T ParseRecord<T>(string? json) where T : KintoneModelBase<T>, new() {
        ArgumentNullException.ThrowIfNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("record", out var recordElement)) {
            throw new InvalidOperationException("Missing 'record' property in JSON.");
        }

        var dict = new Dictionary<string, JsonElement>();
        foreach (var prop in recordElement.EnumerateObject()) {
            dict[prop.Name] = prop.Value;
        }
        var model = new T();
        model.LoadFromJsonDictionary(dict);
        return model;

    }

    /// <summary>
    /// 複数レコードの JSON を解析してモデルのリストに変換する
    /// </summary>
    /// <typeparam name="T">解析対象のモデルの型</typeparam>
    /// <param name="json">解析対象の JSON 文字列</param>
    /// <returns>解析結果のモデルのリスト</returns>
    /// <exception cref="InvalidOperationException">JSON に 'records' プロパティが存在しない場合にスローされます</exception>
    public static IList<T> ParseRecords<T>(string? json) where T : KintoneModelBase<T>, new() {
        ArgumentNullException.ThrowIfNull(json);
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
