namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneフィールドのメタデータを表すクラス
/// </summary>
public class KintoneFieldMetadata {
    /// <summary>
    /// フィールドコード
    /// </summary>
    public string Code { get; set; } = default!;
    /// <summary>
    /// フィールドラベル
    /// </summary>
    public string Label { get; set; } = default!;
    /// <summary>
    /// フィールドタイプ
    /// </summary>
    public KintoneFieldType Type { get; set; }
    /// <summary>
    /// 必須フィールドかどうか
    /// </summary>
    public bool Required { get; set; }

    // 選択肢（ラジオ、ドロップダウン、チェックボックス）
    /// <summary>
    /// 選択肢のリスト
    /// </summary>
    public IReadOnlyList<string>? Options { get; set; }

    // サブテーブルの場合のみ
    /// <summary>
    /// サブフィールドのリスト
    /// </summary>
    public IReadOnlyList<KintoneFieldMetadata>? SubFields { get; set; }
}
