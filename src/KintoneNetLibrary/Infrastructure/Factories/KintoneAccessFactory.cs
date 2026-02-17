using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;

namespace KintoneNetLibrary.Infrastructure.Factories;

/// <summary>
/// KintoneAccessFactoryは、Kintoneへのアクセス方法を提供するファクトリークラスです。
/// 現在はAPIトークンアクセスのみをサポートしていますが、将来的にはユーザーパスワードアクセスなども追加予定です。
/// これにより、Kintoneへのアクセス方法を統一的に管理できるようになります。
/// 例外が発生する可能性のある部分には適切なエラーハンドリングを実装し、将来的な拡張に備えた設計としています。
/// </summary>
public class KintoneAccessFactory : IKintoneAccessFactory {
    /// <summary>
    /// APIトークンアクセスを作成します。
    /// </summary>
    /// <param name="domain">Kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="basicAuthUser">ベーシック認証のユーザー名（省略可能）</param>
    /// <param name="basicAuthPassword">ベーシック認証のパスワード（省略可能）</param>
    /// <param name="guestSpaceId">ゲストスペースID（省略可能）</param>
    /// <returns>作成されたApiTokenAccessインスタンス</returns>
    public ApiTokenAccess CreateApiTokenAccess(string domain, string apiToken, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        return new ApiTokenAccess(domain, apiToken, basicAuthUser, basicAuthPassword, guestSpaceId);
    }

    /// <summary>
    /// ユーザーパスワードアクセスを作成します。
    /// </summary>
    /// <param name="domain">Kintoneのドメイン</param>
    /// <param name="loginName">ログイン名</param>
    /// <param name="password">パスワード</param>
    /// <param name="basicAuthUser">ベーシック認証のユーザー名（省略可能）</param>
    /// <param name="basicAuthPassword">ベーシック認証のパスワード（省略可能）</param>
    /// <param name="guestSpaceId">ゲストスペースID（省略可能）</param>
    /// <returns>作成されたUserPasswordAccessインスタンス</returns>
    /// <exception cref="NotImplementedException">このメソッドはまだ実装されていません。</exception>
    public UserPasswordAccess CreateBasicAuthAccess(string domain, string loginName, string password, string basicAuthUser = "", string basicAuthPassword = "", int guestSpaceId = 0) {
        throw new NotImplementedException();
    }
}