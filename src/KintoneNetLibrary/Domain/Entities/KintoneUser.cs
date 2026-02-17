namespace KintoneNetLibrary.Domain.Entities;

// コメントは日本語で記述
/// <summary>
/// Kintoneのユーザー情報を表すクラス
/// </summary>
public class KintoneUser {
    /// <summary>
    /// ユーザーコード
    /// </summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// ユーザー名
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
