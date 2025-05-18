using System.Collections.Generic;

namespace KintoneNetLibrary.Types;

/// <summary>
/// 一括登録・更新などで複数レコード分の結果を返す DTO.
/// </summary>
public class KintoneIndexes
{
    /// <summary>
    /// レコード ID リスト
    /// </summary>
    public IList<string> IDs { get; set; } = new List<string>();

    /// <summary>
    /// 各レコードのリビジョン番号リスト
    /// </summary>
    public IList<int> Revisions { get; set; } = new List<int>();
}
