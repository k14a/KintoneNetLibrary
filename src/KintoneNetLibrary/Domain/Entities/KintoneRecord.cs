using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのレコードを表すクラス
/// </summary>
public class KintoneRecord
{
    /// <summary>
    /// レコードのフィールドを表す辞書
    /// </summary>
    [JsonPropertyName("record")]
    public Dictionary<string, KintoneField> Fields { get; set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public KintoneRecord() {
        this.Fields = [];
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="fields"></param>
    public KintoneRecord(Dictionary<string, KintoneField> fields) {
        this.Fields = fields;
    }
}
