namespace KintoneNetLibrary.CodeGen.Domain.Schemas;

/// <summary>
/// Kintone アプリスキーマ
/// </summary>
public class KintoneAppSchema {
    /// <summary>
    /// アプリID
    /// </summary>
    public int AppId { get; set; }

    /// <summary>
    /// アプリ名（クラス名のデフォルトに使用）
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// 通常フィールド（サブテーブル以外）
    /// </summary>
    public List<KintoneFieldSchema> Fields { get; set; } = [];

    /// <summary>
    /// サブテーブル一覧
    /// </summary>
    public List<KintoneSubtableSchema> Subtables { get; set; } = [];
}