namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 名前変換インターフェース
/// </summary>
public interface INameConverter {
    /// <summary>
    /// クラス名に変換する
    /// </summary>
    /// <param name="label">Kintone フィールドのラベル</param>
    /// <param name="code">Kintone フィールドのコード</param>
    /// <param name="tableTemplate">サブテーブルテンプレートかどうか</param>
    /// <returns>変換後のクラス名</returns>
    string ToClassName(string label, string code, bool tableTemplate = false);

    /// <summary>
    /// プロパティ名に変換する
    /// </summary>
    /// <param name="label">Kintone フィールドのラベル</param>
    /// <param name="code">Kintone フィールドのコード</param>
    /// <param name="tableTemplate">サブテーブルテンプレートかどうか</param>
    /// <returns>変換後のプロパティ名</returns>
    string ToPropertyName(string label, string code, bool tableTemplate = false);
}

