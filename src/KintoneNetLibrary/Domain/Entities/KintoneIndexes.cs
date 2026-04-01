using System.Text.Json;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのインデックスを表すクラス
/// このクラスは、KintoneのレコードのIDとリビジョンを保持します。
/// 単一レコードの ID / Revision を表す DTO.
/// </summary>
public class KintoneIndexes {
    /// <summary>
    /// レコード ID リスト
    /// </summary>
    [JsonPropertyName("ids")]
    public IList<string?> IDs { get; set; } = [];

    /// <summary>
    /// 各レコードのリビジョン番号リスト
    /// </summary>
    [JsonPropertyName("revisions")]
    public IList<string?> Revisions { get; set; } = [];

    /// <summary>
    /// JSON文字列からKintoneIndexesを生成する
    /// </summary>
    /// <param name="json">Kintone APIのレスポンス（JSON）</param>
    /// <returns>KintoneIndexesインスタンス</returns>
    public static KintoneIndexes Parse(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("JSON string is null or empty", nameof(json));
        }

        return JsonSerializer.Deserialize<KintoneIndexes>(json) ?? new KintoneIndexes();
    }

    /// <summary>
    /// KintoneIndexesからKintoneIndexの一覧に変換する
    /// </summary>
    /// <returns>List&lt;KintoneIndex&gt;</returns>
    public IList<KintoneIndex> ToIndexList() {
        var list = new List<KintoneIndex>();

        for (int i = 0; i < Math.Max(this.IDs.Count, this.Revisions.Count); i++) {
            var id = i < this.IDs.Count ? this.IDs[i] ?? "" : "";
            var revision = i < this.Revisions.Count ? this.Revisions[i] ?? "-1" : "-1";
            list.Add(new KintoneIndex {
                ID = id,
                RevisionString = revision
            });
        }

        return list;
    }

}
