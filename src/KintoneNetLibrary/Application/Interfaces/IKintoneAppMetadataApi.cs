using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintoneアプリのメタデータ取得APIインターフェース
/// </summary>
public interface IKintoneAppMetadataApi {
    /// <summary>
    /// アプリのフィールド情報をJSON形式で取得する
    /// </summary>
    /// <param name="domain">Kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリのId</param>
    /// <returns>フィールド情報のJSON文字列</returns>
    Task<string> GetFieldsJsonAsync(string domain, string apiToken, int appId);
    /// <summary>
    /// アプリのレイアウト情報をJSON形式で取得する
    /// </summary>
    /// <param name="domain">Kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリのId</param>
    /// <returns>レイアウト情報のJSON文字列</returns>
    Task<string> GetLayoutJsonAsync(string domain, string apiToken, int appId);
    /// <summary>
    /// アプリのメタデータを取得する
    /// </summary>
    /// <param name="domain">Kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリのId</param>
    /// <returns>アプリのメタデータ</returns>
    Task<KintoneAppMetadata> GetAppMetadataAsync(string domain, string apiToken, int appId);
}
