using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのrecords.json APIからのレスポンスを格納する型付きレスポンスクラス。
/// </summary>
public class KintoneRecordsResponse<T> {
    /// <summary>
    /// レコード配列。TはKintoneModelBaseを継承したモデル。
    /// </summary>
    [JsonPropertyName("records")]
    public List<T> Records { get; set; } = new();

    /// <summary>
    /// オプション：totalCount=trueを指定した場合のみ返る。
    /// </summary>
    [JsonPropertyName("totalCount")]
    public string? TotalCountRaw { get; set; }

    /// <summary>
    /// 数値型に変換したtotalCount。nullの場合は未取得。
    /// </summary>
    [JsonIgnore]
    public int? TotalCount => int.TryParse(this.TotalCountRaw, out var count) ? count : null;
}
