using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのレコードレスポンス
/// </summary>
/// <typeparam name="T"></typeparam>
public class KintoneRecordResponse<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// レコード情報
    /// </summary>
    [JsonPropertyName("record")]
    public T Record { get; set; } = default!;
    /// <summary>
    /// レコードのリビジョン番号
    /// </summary>
    [JsonPropertyName("revision")]
    public string? RevisionRaw { get; set; }
    /// <summary>
    /// レコードのリビジョン番号（数値型）
    /// </summary>
    [JsonIgnore()]
    public int? Revision => int.TryParse(this.RevisionRaw, out var revision) ? revision : null;
}