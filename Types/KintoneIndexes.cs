using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Types;

/// <summary>
/// 一括登録・更新などで複数レコード分の結果を返す DTO.
/// </summary>
public class KintoneIndexes
{
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
}
