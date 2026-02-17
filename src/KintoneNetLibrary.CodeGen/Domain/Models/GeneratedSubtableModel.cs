namespace KintoneNetLibrary.CodeGen.Domain.Models;

/// <summary>
/// サブテーブルモデルの生成結果
/// </summary>
public class GeneratedSubTableModel {
    /// <summary>
    /// 生成されたサブテーブル行クラス名
    /// </summary>
    public string ClassName { get; set; } = "";

    /// <summary>
    /// 生成された C# コード全文
    /// </summary>
    public string Code { get; set; } = "";
}