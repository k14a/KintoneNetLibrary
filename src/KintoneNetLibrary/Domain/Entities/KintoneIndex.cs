using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのインデックスを表すクラス
/// このクラスは、KintoneのレコードのIDとリビジョンを保持します。
/// </summary>
public class KintoneIndex {
    /// <summary>
    /// レコードID
    /// このプロパティは、Kintoneのレコードの一意の識別子を表します。
    /// レコードを取得または更新する際に使用されます。
    /// </summary>
    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// レコードのリビジョン
    /// このプロパティは、Kintoneのレコードのバージョンを表します。
    /// レコードの更新時に、リビジョンが一致することを確認するために使用されます。
    /// </summary>
    [JsonPropertyName("revision")]
    public string RevisionString { get; set; } = "-1";

    /// <summary>
    /// レコードのリビジョン
    /// このプロパティは、Kintoneのレコードのバージョンを整数として表します。
    /// 文字列形式のリビジョンを整数に変換して返します。
    /// もし変換に失敗した場合は、-1を返します。
    /// このプロパティは、リビジョンの整数値を直接取得または設定するために使用されます。
    /// </summary>
    [JsonIgnore()]
    public int Revision {
        get => int.TryParse(this.RevisionString, out var revision) ? revision : -1;
        set => this.RevisionString = value.ToString();
    }
}
