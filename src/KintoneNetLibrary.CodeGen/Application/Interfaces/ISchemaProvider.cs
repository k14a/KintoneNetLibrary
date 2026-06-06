using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// Kintoneアプリのスキーマ情報を提供するプロバイダーインターフェース
/// </summary>
public interface ISchemaProvider {
    /// <summary>
    /// 指定されたアプリIDとAPIトークンに基づいてKintoneアプリのスキーマ情報を取得します。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリID</param>
    /// <returns>Kintoneアプリのスキーマ情報</returns>
    Task<KintoneAppSchema> GetSchemaAsync(string domain, string apiToken, int appId);

    /// <summary>
    /// 指定されたアプリIDとAPIトークンに基づいてKintoneアプリのメタデータを取得します。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリID</param>
    /// <returns>Kintoneアプリのメタデータ</returns>
    Task<KintoneAppMetadata> GetMetadataAsync(string domain, string apiToken, int appId);

    /// <summary>
    /// 指定されたバックアップスキーマと現在のスキーマを比較し、差分を取得します。
    /// </summary>
    /// <param name="backupSchema">バックアップスキーマ</param>
    /// <param name="domain">Kintoneのサブドメイン</param>
    /// <param name="appId">アプリID</param>
    /// <param name="apiToken">APIトークン</param>
    /// <returns>スキーマの差分リスト</returns>
    Task<IReadOnlyList<KintoneMetadataDiff>> CompareAsync(KintoneAppMetadata backupSchema, string domain, string apiToken, int appId);
}