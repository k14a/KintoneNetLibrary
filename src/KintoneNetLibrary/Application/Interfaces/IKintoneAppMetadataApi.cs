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
}
