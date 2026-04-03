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
    /// Kintone ゲストスペースID を取得する
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
    /// Kintone アプリIDを抽出する
    /// </summary>
    /// <returns>アプリID</returns>
    protected virtual int ExtractAppID() => throw new NotImplementedException();

    /// <summary>
    /// Kintone ゲストアプリIDを抽出する
    /// </summary>
    /// <returns>ゲストアプリID</returns>
    protected virtual int ExtractGuestAppID() => throw new NotImplementedException();

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
