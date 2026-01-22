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
    protected readonly KintoneAccessBase Access;
    /// <summary>
    /// HTTPクライアント
    /// </summary>
    protected readonly HttpClient HttpClient;
    /// <summary>
    /// ロガー
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="access"></param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    protected BaseKintoneApi(KintoneAccessBase access, HttpClient httpClient, ILogger? logger = null) {
        this.Access = access;
        this.HttpClient = httpClient;
        this.Logger = logger;

        this.EnsureDefaultHeaders();
    }

    /// <summary>
    /// デフォルトヘッダーの設定を確認・追加
    /// </summary>
    protected void EnsureDefaultHeaders() {
        if (!this.HttpClient.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json")) {
            this.HttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (!string.IsNullOrEmpty(this.Access.ApiToken) && !this.HttpClient.DefaultRequestHeaders.Contains("X-Cybozu-API-Token")) {
            this.HttpClient.DefaultRequestHeaders.Add("X-Cybozu-API-Token", this.Access.ApiToken);
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
        if (string.IsNullOrWhiteSpace(this.Access.Domain)) { throw new InvalidOperationException("Domain is not set."); }

        var baseUri = new Uri($"https://{this.Access.Domain.TrimEnd('/')}/k/v1/");
        var builder = new UriBuilder(new Uri(baseUri, path));

        if (!string.IsNullOrEmpty(query)) { builder.Query = query; }

        return builder.Uri;
    }

    /// <summary>
    /// 認証情報の適用
    /// </summary>
    /// <param name="request"></param>
    protected void ApplyAuth(HttpRequestMessage request) {
        this.Access.ApplyAuthentication(request);
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

        using var response = await this.HttpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        this.Logger?.LogTrace(json);

        if (!response.IsSuccessStatusCode) { throw new KintoneException(KintoneErrorConverter.Parse(json)); }

        return json;
    }
}
