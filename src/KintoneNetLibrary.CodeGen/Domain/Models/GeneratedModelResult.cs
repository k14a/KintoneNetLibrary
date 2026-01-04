namespace KintoneNetLibrary.CodeGen.Domain.Models;

/// <summary>
/// CSharpCodeEmitter が生成したモデルコード一式
/// </summary>
public class GeneratedModelResult {
    /// <summary>
    /// メインモデル（アプリ本体）の C# コード
    /// </summary>
    public string MainModelCode { get; set; } = "";

    /// <summary>
    /// サブテーブル行クラスの生成結果一覧
    /// </summary>
    public List<GeneratedSubtableModel> SubtableModels { get; set; } = [];

    /// <summary>
    /// pure モードで生成される補助クラス（UserInfo など）
    /// </summary>
    public List<GeneratedHelperClass> HelperClasses { get; set; } = [];

    /// <summary>
    /// Kintone アプリのフィールド定義のリビジョン番号
    /// </summary>
    public int Revision { get; set; }
}