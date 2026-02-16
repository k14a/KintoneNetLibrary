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
    /// <param name="domain"></param>
    /// <param name="apiToken"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<KintoneAppSchema> GetSchemaAsync(string domain, string apiToken, int appId);
    /// <summary>
    /// Kintoneのサブドメインを設定します。
    /// </summary>
    /// <param name="subDomain"></param>
    void SetDomain(string subDomain);
    /// <summary>
    /// 指定されたアプリIDとAPIトークンに基づいてKintoneアプリのメタデータを取得します。
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="apiToken"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<KintoneAppMetadata> GetMetadataAsync(string domain, string apiToken, int appId);
    /// <summary>
    /// 指定されたバックアップスキーマと現在のスキーマを比較し、差分を取得します。
    /// </summary>
    /// <param name="backupSchema"></param>
    /// <param name="domain"></param>
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<KintoneMetadataDiff>> CompareAsync(KintoneAppMetadata backupSchema, string domain, string apiToken, int appId);
}