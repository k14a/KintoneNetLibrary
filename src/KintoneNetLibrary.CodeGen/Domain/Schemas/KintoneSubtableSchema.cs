namespace KintoneNetLibrary.CodeGen.Domain.Schemas;

/// <summary>
/// Kintone サブテーブルスキーマ
/// </summary>
public class KintoneSubtableSchema {
    /// <summary>
    /// サブテーブルのフィールドコード（例: "book_details"）
    /// </summary>
    public string FieldCode { get; set; } = "";

    /// <summary>
    /// 表示名（例: "書籍明細"）
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// サブテーブル内のフィールド一覧
    /// </summary>
    public List<KintoneFieldSchema> Fields { get; set; } = [];
}