using System.Net.Http.Headers;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Infrastructure.Api;

// コメントは日本語で記述
/// <summary>
/// Kintone APIの基底クラス
/// </summary>
public abstract class BaseKintoneApi {
    /// <summary>
    /// Kintoneアクセス情報
    /// </summary>
    private readonly KintoneAccessBase _access;
    /// <summary>
    /// HTTPクライアント
    /// </summary>
    private readonly HttpClient _httpClient;
    /// <summary>
    /// ロガー
    /// </summary>
    private readonly ILogger? _logger;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="access"></param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public BaseKintoneApi(KintoneAccessBase access, HttpClient httpClient, ILogger? logger = null) {
        this._access = access;
        this._httpClient = httpClient;
        this._logger = logger;

        this.EnsureDefaultHeaders();
    }

    /// <summary>
    /// デフォルトヘッダーの設定を確認・追加
    /// </summary>
    protected void EnsureDefaultHeaders() {
        if (!this._httpClient.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json")) {
            this._httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (!string.IsNullOrEmpty(this._access.ApiToken) && !this._httpClient.DefaultRequestHeaders.Contains("X-Cybozu-API-Token")) {
            this._httpClient.DefaultRequestHeaders.Add("X-Cybozu-API-Token", this._access.ApiToken);
        }
    }

    /// <summary>
    /// リクエストURIの構築
    /// </summary>
    /// <param name="path"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected Uri BuildRequestUri(string path, string? query = null) {
        if (string.IsNullOrWhiteSpace(this._access.Domain)) { throw new InvalidOperationException("Domain is not set."); }

        var baseUri = new Uri($"https://{this._access.Domain.TrimEnd('/')}/k/v1/");
        var builder = new UriBuilder(new Uri(baseUri, path));

        if (!string.IsNullOrEmpty(query)) { builder.Query = query; }

        return builder.Uri;
    }

    /// <summary>
    /// 認証情報の適用
    /// </summary>
    /// <param name="request"></param>
    protected void ApplyAuth(HttpRequestMessage request) {
        this._access.ApplyAuthentication(request);
    }

    /// <summary>
    /// GETリクエストの送信
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    /// <exception cref="KintoneException"></exception>
    protected async Task<string> SendGetAsync(Uri uri) {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        this.ApplyAuth(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        this._logger?.LogTrace(json);

        if (!response.IsSuccessStatusCode) { throw new KintoneException(KintoneErrorConverter.Parse(json)); }

        return json;
    }
}
