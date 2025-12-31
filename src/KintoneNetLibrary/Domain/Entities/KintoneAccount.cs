namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone アカウント情報
/// </summary>
public class KintoneAccount {
    /// <summary>
    /// Kintone ドメイン
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Kintone ログインユーザ名
    /// </summary>
    public string LoginName { get; set; } = string.Empty;

    /// <summary>
    /// Kintone パスワード
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Kintone APIトークン
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Kintone Basic認証用ユーザ名
    /// </summary>
    public string BasicAuthUser { get; set; } = string.Empty;

    /// <summary>
    /// Kintone Basic認証用パスワード
    /// </summary>
    public string BasicAuthPassword { get; set; } = string.Empty;

    /// <summary>
    /// Kintone ゲストスペースID
    /// </summary>
    public int GuestSpaceId { get; set; } = 0;

    /// <summary>
    /// Kintone ログイン用URLを取得する
    /// </summary>
    /// <returns>ログインURL</returns>
    public string GetLoginUrl() {
        var url = $"https://{this.Domain}/login";
        if (this.GuestSpaceId > 0) {
            url += $"/guest/{this.GuestSpaceId}";
        }
        return url;
    }

    /// <summary>
    /// Kintone アカウントがAPIトークン認証を使用しているかどうかを確認する
    /// </summary>
    /// <returns>APIトークン認証を使用している場合はtrue、それ以外はfalse</returns>
    public bool HasApiTokenAuth => !string.IsNullOrEmpty(this.ApiToken);

    /// <summary>
    /// Kintone アカウントがパスワード認証を使用しているかどうかを確認する
    /// </summary>
    /// <returns>パスワード認証を使用している場合はtrue、それ以外はfalse</returns>
    public bool HasPasswordAuth => !string.IsNullOrEmpty(this.LoginName) && !string.IsNullOrEmpty(this.Password);

    /// <summary>
    /// Kintone アカウントがBasic認証を使用しているかどうかを確認する
    /// </summary>
    /// <returns>Basic認証を使用している場合はtrue、それ以外はfalse</returns>
    public bool HasBasicAuth => !string.IsNullOrEmpty(this.BasicAuthUser) && !string.IsNullOrEmpty(this.BasicAuthPassword);
}
