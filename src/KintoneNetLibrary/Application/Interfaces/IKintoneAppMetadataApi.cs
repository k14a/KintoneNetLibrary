using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintoneアプリのメタデータ取得APIインターフェース
/// </summary>
public interface IKintoneAppMetadataApi {
    /// <summary>
    /// アプリのフィールド情報をJSON形式で取得する
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<string> GetFieldsJsonAsync(int appId);
    /// <summary>
    /// アプリのレイアウト情報をJSON形式で取得する
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<string> GetLayoutJsonAsync(int appId);
    /// <summary>
    /// アプリのメタデータを取得する
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    Task<KintoneAppMetadata> GetAppMetadataAsync(int appId, string apiToken);
}
