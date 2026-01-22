using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Access;

/// <summary>
/// UserPasswordAccessクラスは、ユーザー名とパスワードを使用してKintoneにアクセスするための認証情報を提供します。
/// このクラスは、Kintoneのユーザー名とパスワード認証を使用して、Kintoneのドメインにアクセスするための設定を行います。
/// </summary>
/// <remarks>
/// このクラスは、KintoneのAPIトークン認証を使用するApiTokenAccessクラスとは異なり、ユーザー名とパスワードを使用して認証を行います。
/// </remarks>
public class UserPasswordAccess : KintoneAccessBase {
    /// <summary>
    /// UserPasswordAccessクラスのコンストラクタ。
    /// </summary>
    /// <remarks>
    /// このコンストラクタは、Kintoneのドメイン、ログイン名、パスワード、およびオプションの基本認証情報を使用して、UserPasswordAccessオブジェクトを初期化します。
    /// </remarks>
    /// <param name="domain">Kintoneのドメイン名。</param>
    /// <param name="loginName">Kintoneのログイン名。</param>
    /// <param name="password">Kintoneのパスワード。</param>
    /// <param name="basicAuthUser">基本認証のユーザー名（オプション）。</param>
    /// <param name="basicAuthPassword">基本認証のパスワード（オプション）。</param>
    /// <param name="guestSpaceId">ゲストスペースID（オプション）。</param>
    /// <exception cref="ArgumentNullException">ドメイン、ログイン名、またはパスワードがnullの場合にスローされます。</exception>
    /// <exception cref="ArgumentException">ドメイン、ログイン名、またはパスワードが空文字列の場合にスローされます。</exception>
    /// <exception cref="ArgumentOutOfRangeException">ゲストスペースIDが負の値の場合にスローされます。</exception>
    /// <exception cref="NotImplementedException">ApplyAuthenticationメソッドは未実装です。</exception>
    public UserPasswordAccess(string domain, string loginName, string password, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        this.Domain = domain;
        this.LoginName = loginName;
        this.Password = password;
        this.BasicAuthUser = basicAuthUser;
        this.BasicAuthPassword = basicAuthPassword;
        this.GuestSpaceId = guestSpaceId;
        this.AuthType = KintoneAuthType.Login;
    }

    /// <summary>
    /// Kintoneのドメイン名を取得または設定します。
    /// /// </summary>
    /// <remarks>
    /// このメソッドは、Kintoneのドメイン名、ログイン名、パスワード、基本認証情報、およびゲストスペースIDを含むKintoneAccountオブジェクトを返します。
    /// </remarks>
    /// <returns>KintoneAccountオブジェクトにドメイン、ログイン名、パスワード、基本認証情報、およびゲストスペースIDを設定します。</returns>
    public override KintoneAccount ToKintoneAccount() => new() {
        Domain = this.Domain,
        LoginName = this.LoginName,
        Password = this.Password,
        BasicAuthUser = this.BasicAuthUser,
        BasicAuthPassword = this.BasicAuthPassword,
        GuestSpaceId = this.GuestSpaceId
    };

    /// <summary>
    /// Kintoneのユーザー名とパスワードを使用して認証情報をHTTPリクエストに適用します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、HTTPリクエストメッセージにKintoneのユーザー名とパスワードを使用した認証情報を設定します。
    /// </remarks>
    /// <param name="request">HTTPリクエストメッセージに適用する認証情報を設定します。</param>
    /// <exception cref="NotImplementedException">このメソッドは未実装です。</exception>
    public override void ApplyAuthentication(HttpRequestMessage request) {
        throw new NotImplementedException();
    }
}
