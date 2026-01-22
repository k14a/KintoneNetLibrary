using System.Net.Http.Headers;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Access;

/// <summary>
/// ApiTokenAccessクラスは、APIトークンを使用してKintoneにアクセスするための認証情報を提供します。
/// このクラスは、KintoneのAPIトークン認証を使用して、Kintoneのドメインにアクセスするための設定を行います。
/// </summary>
public class ApiTokenAccess : KintoneAccessBase {
    /// <summary>
    /// ApiTokenAccessクラスのコンストラクタ。
    /// </summary>
    /// /// <param name="domain">Kintoneのドメイン名。</param>
    /// <param name="apiToken">APIトークン。</param>
    /// /// <param name="basicAuthUser">基本認証のユーザー名（オプション）。</param>
    /// <param name="basicAuthPassword">基本認証のパスワード（オプション）。</param>
    /// <param name="guestSpaceId">ゲストスペースID（オプション）。</param>
    public ApiTokenAccess(string domain, string apiToken, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        this.Domain = domain;
        this.ApiToken = apiToken;
        this.BasicAuthUser = basicAuthUser;
        this.BasicAuthPassword = basicAuthPassword;
        this.GuestSpaceId = guestSpaceId;
        this.AuthType = KintoneAuthType.ApiToken;
    }

    /// <summary>
    /// Kintoneのドメイン名を取得または設定します。
    /// </summary>
    public override KintoneAccount ToKintoneAccount() => new() {
        Domain = this.Domain,
        ApiToken = this.ApiToken,
        BasicAuthUser = this.BasicAuthUser,
        BasicAuthPassword = this.BasicAuthPassword,
        GuestSpaceId = this.GuestSpaceId
    };

    /// <summary>
    /// KintoneのAPIトークンを取得または設定します。
    /// </summary>
    /// <remarks>
    /// APIトークンは、Kintoneのアプリケーションにアクセスするための認証情報です。
    /// </remarks>
    /// <param name="request">HTTPリクエストメッセージに適用する認証情報を設定します。</param>
    public override void ApplyAuthentication(HttpRequestMessage request) {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(this.ApiToken)) {
            request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        }
        // 他にも必要なヘッダーを設定
    }
}
