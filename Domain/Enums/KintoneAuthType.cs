namespace KintoneNetLibrary.Domain.Enums;

/// <summary>
/// Kintone認証方法
/// </summary>
public enum KintoneAuthType {
    /// <summary>
    /// APIトークン認証
    /// </summary>
    ApiToken,
    /// <summary>
    /// ユーザ名・パスワード認証
    /// </summary>
    Login,
    /// <summary>
    /// Basic認証
    /// </summary>
    Basic
}
