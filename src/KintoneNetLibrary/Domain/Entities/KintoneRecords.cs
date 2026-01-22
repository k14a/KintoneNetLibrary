using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

// コメントは日本語で記述してください
/// <summary>
/// Kintoneのレコード群を表すクラス
/// </summary>
/// <typeparam name="T"></typeparam>
public class KintoneRecords<T> {
    /// <summary>
    /// アプリのID
    /// </summary>
    [JsonPropertyName("app")]
    public int AppID { get; set; }
    /// <summary>
    /// レコードのID一覧
    /// </summary>
    [JsonPropertyName("ids")]
    public List<string>? IDs { get; set; }

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