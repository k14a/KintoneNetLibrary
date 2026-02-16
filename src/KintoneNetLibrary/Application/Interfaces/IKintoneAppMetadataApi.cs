using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintoneアプリのメタデータ取得APIインターフェース
/// </summary>
public interface IKintoneAppMetadataApi {
    /// <summary>
    /// アプリのフィールド情報をJSON形式で取得する
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="apiToken"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<string> GetFieldsJsonAsync(string domain, string apiToken, int appId);
    /// <summary>
    /// アプリのレイアウト情報をJSON形式で取得する
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="apiToken"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<string> GetLayoutJsonAsync(string domain, string apiToken, int appId);
    /// <summary>
    /// アプリのメタデータを取得する
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="apiToken"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<KintoneAppMetadata> GetAppMetadataAsync(string domain, string apiToken, int appId);
}
