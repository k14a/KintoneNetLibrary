using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Infrastructure.Internal;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Infrastructure.Api;

// コメントは日本語で記述
/// <summary>
/// Kintoneアプリのメタデータを取得するAPIクラス
/// </summary>
public class KintoneAppMetadataApi : BaseKintoneApi, IKintoneAppMetadataApi {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="access"></param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public KintoneAppMetadataApi(
        KintoneAccessBase access,
        HttpClient httpClient,
        ILogger<KintoneAppMetadataApi>? logger = null)
        : base(access, httpClient, logger) { }

    /// <summary>
    /// 指定したアプリのフィールド情報をJSON形式で取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    public async Task<string> GetFieldsJsonAsync(int appId) {
        var uri = this.BuildRequestUri(KintoneApiEndpoints.GetAppFields, $"app={appId}");
        return await this.SendGetAsync(uri);
    }

    /// <summary>
    /// 指定したアプリのレイアウト情報をJSON形式で取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    public async Task<string> GetLayoutJsonAsync(int appId) {
        var uri = this.BuildRequestUri(KintoneApiEndpoints.GetAppLayout, $"app={appId}");
        return await this.SendGetAsync(uri);
    }
}
