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
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    Task<KintoneAppSchema> GetSchemaAsync(int appId, string apiToken);
    /// <summary>
    /// Kintoneのサブドメインを設定します。
    /// </summary>
    /// <param name="subDomain"></param>
    void SetDomain(string subDomain);
    /// <summary>
    /// 指定されたアプリIDとAPIトークンに基づいてKintoneアプリのメタデータを取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    Task<KintoneAppMetadata> GetMetadataAsync(int appId, string apiToken);
}