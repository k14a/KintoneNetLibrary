namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 名前変換インターフェース
/// </summary>
public interface INameConverter {
    /// <summary>
    /// クラス名に変換する
    /// </summary>
    /// <param name="label"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    string ToClassName(string label, string code);
    /// <summary>
    /// プロパティ名に変換する
    /// </summary>
    /// <param name="label"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    string ToPropertyName(string label, string code);
}

