using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

// コメントは日本語で記述してください
/// <summary>
/// Kintoneのレコード群を表すクラス
/// </summary>
/// <typeparam name="T"></typeparam>
public class KintoneRecords<T> {
    /// <summary>
    /// アプリのId
    /// </summary>
    [JsonPropertyName("app")]
    public int AppId { get; set; }
    /// <summary>
    /// レコードのId一覧
    /// </summary>
    [JsonPropertyName("ids")]
    public List<string>? Ids { get; set; }

    /// <summary>
    /// レコードのリビジョン番号一覧
    /// </summary>
    [JsonPropertyName("revisions")]
    public List<int>? Revisions { get; set; }

    /// <summary>
    /// レコードのリスト
    /// </summary>
    [JsonPropertyName("records")]
    public List<T> Records { get; set; } = new();
}