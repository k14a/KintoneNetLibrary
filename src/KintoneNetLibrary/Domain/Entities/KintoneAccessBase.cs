using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone アクセスの基本クラス
/// </summary>
public abstract class KintoneAccessBase {
    private string _domain = string.Empty;

    /// <summary>
    /// Kintone 接続情報を生成する
    /// </summary>
    public abstract KintoneAccount ToKintoneAccount();

    /// <summary>
    /// Kintone ドメイン を取得する
    /// </summary>
    public virtual string Domain {
        get => _domain;
        set => _domain = NormalizeDomain(value);
    }

    /// <summary>
    /// Kintone Basic認証用ユーザ名 を取得する
    /// </summary>
    public virtual string BasicAuthUser { get; set; } = string.Empty;

    /// <summary>
    /// Kintone Basic認証用パスワード を取得する
    /// </summary>
    public virtual string BasicAuthPassword { get; set; } = string.Empty;

    /// <summary>
    /// Kintone APIトークン を取得する
    /// </summary>
    public virtual string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Kintone ログイン名 を取得する
    /// </summary>
    public virtual string LoginName { get; set; } = string.Empty;

    /// <summary>
    /// Kintone パスワード を取得する
    /// </summary>
    public virtual string Password { get; set; } = string.Empty;

    /// <summary>
    /// Kintone ゲストスペースId を取得する
    /// </summary>
    public virtual int GuestSpaceId { get; set; }

    /// <summary>
    /// Kintone 認証タイプを取得する
    /// </summary>
    public KintoneAuthType AuthType { get; protected set; }

    /// <summary>
    /// Kintone アクセスの認証を適用する
    /// </summary>
    /// <param name="request">HTTPリクエスト</param>
    public abstract void ApplyAuthentication(HttpRequestMessage request);

    /// <summary>
    /// Kintone アプリIdを抽出する
    /// </summary>
    /// <returns>アプリId</returns>
    protected virtual int ExtractAppId() => throw new NotImplementedException();

    /// <summary>
    /// Kintone ゲストアプリIdを抽出する
    /// </summary>
    /// <returns>ゲストアプリId</returns>
    protected virtual int ExtractGuestAppId() => throw new NotImplementedException();

    /// <summary>
    /// サブドメインまたはドメインを正規化する
    /// </summary>
    /// <param name="subDomainOrDomain">サブドメインまたはドメイン</param>
    /// <returns>正規化されたドメイン</returns>
    private static string NormalizeDomain(string subDomainOrDomain) {
        if (string.IsNullOrWhiteSpace(subDomainOrDomain)) {
            return string.Empty;
        }

        if (subDomainOrDomain.Contains('.')) {
            return subDomainOrDomain;
        } else {
            return $"{subDomainOrDomain}.cybozu.com";
        }
    }
}
