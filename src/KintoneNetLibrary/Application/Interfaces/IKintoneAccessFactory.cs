using KintoneNetLibrary.Domain.Access;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintoneアクセスのファクトリインターフェース
/// </summary>
public interface IKintoneAccessFactory {
    /// <summary>
    /// APIトークンアクセスを生成する
    /// </summary>
    /// <param name="domain">kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="basicAuthUser">ベーシック認証のユーザー名（オプション）</param>
    /// <param name="basicAuthPassword">ベーシック認証のパスワード（オプション）</param>
    /// <param name="guestSpaceId">ゲストスペースID（オプション）</param>
    /// <returns>生成されたAPIトークンアクセス</returns>
    ApiTokenAccess CreateApiTokenAccess(string domain, string apiToken, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0);
    /// <summary>
    /// ベーシック認証アクセスを生成する
    /// </summary>
    /// <param name="domain">kintoneのドメイン</param>
    /// <param name="loginName">ログイン名</param>
    /// <param name="password">パスワード</param>
    /// <param name="basicAuthUser">ベーシック認証のユーザー名（オプション）</param>
    /// <param name="basicAuthPassword">ベーシック認証のパスワード（オプション）</param>
    /// <param name="guestSpaceId">ゲストスペースID（オプション）</param>
    /// <returns>生成されたベーシック認証アクセス</returns>
    UserPasswordAccess CreateBasicAuthAccess(string domain, string loginName, string password, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0);
}