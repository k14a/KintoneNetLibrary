using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Application.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Api;
// コメントは日本語で記述
/// <summary>
/// Kintone API 基底クラス
/// </summary>
public partial class KintoneApi : IKintoneApi, IDisposable {
    #region <<Private values>>
    private readonly KintoneAccessBase _access;
    private readonly int _appID;
    private HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<KintoneApi>? _logger;
    private int _cursorPageSize = KintoneConstants.CursorFetchLimit;
    private long _maxUploadFileSize = KintoneConstants.MaxUploadFileSize;
    private int _maxUploadFileCount = KintoneConstants.MaxUploadFileCount;
    #endregion

    #region <<Properties>>
    /// <summary>
    /// カーソルAPIで一度に取得する件数(省略時はKintoneの最大値である500)
    /// </summary>
    public int CursorPageSize {
        get => this._cursorPageSize;
        set {
            if (value <= 0 || value > KintoneConstants.CursorFetchLimit) {
                throw new ArgumentOutOfRangeException(nameof(this.CursorPageSize), value, $"CursorPageSizeは1以上{KintoneConstants.CursorFetchLimit}以下でなければなりません。");
            }
            this._cursorPageSize = value;
        }
    }
    /// <summary>
    /// アップロード可能なファイルサイズ(省略時はKintoneの最大値である100MB)
    /// </summary>
    public long MaxUploadFileSize {
        get => this._maxUploadFileSize;
        set {
            if (value <= 0 || value > KintoneConstants.MaxUploadFileSize) {
                throw new ArgumentOutOfRangeException(nameof(this.MaxUploadFileSize), value, $"MaxUploadFileSize は 1〜{KintoneConstants.MaxUploadFileSize}（{KintoneConstants.MaxUploadFileSize / 1024 / 1024}MB）までの値でなければなりません。");
            }
            this._maxUploadFileSize = value;
        }
    }
    /// <summary>
    /// アップロード可能なファイル数(省略時はKintoneの最大値である20)
    /// </summary>
    public int MaxUploadFileCount {
        get => this._maxUploadFileCount;
        set {
            if (value <= 0 || value > KintoneConstants.MaxUploadFileCount) {
                throw new ArgumentOutOfRangeException(nameof(this.MaxUploadFileCount), value,
                    $"MaxUploadFileCount は 1〜{KintoneConstants.MaxUploadFileCount} の間でなければなりません。");
            }
            this._maxUploadFileCount = value;
        }
    }
    #endregion

    #region <<Constructor(s)>>
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="access">KintoneAccessBase</param>
    /// <param name="appID">Kintone Application ID</param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    /// <param name="jsonOptions"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public KintoneApi(
        KintoneAccessBase access,
        int appID,
        HttpClient? httpClient = null,
        ILogger<KintoneApi>? logger = null,
        JsonSerializerOptions? jsonOptions = null) {

        ArgumentNullException.ThrowIfNull(access);

        this._access = access;
        this._appID = appID;
        this._logger = logger;
        this._httpClient = httpClient ?? new HttpClient();
        this._jsonOptions = jsonOptions ?? DefaultJsonOptions.Default;

        if (!string.IsNullOrWhiteSpace(this._access.Domain)) {
            this._httpClient.BaseAddress = new Uri($"https://{this._access.Domain.TrimEnd('/')}/k/v1/");
        }

        this.EnsureDefaultHeaders();
    }
    #endregion

    private void EnsureDefaultHeaders() {
        if (!this._httpClient.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json")) {
            this._httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (!string.IsNullOrEmpty(this._access.ApiToken) && !this._httpClient.DefaultRequestHeaders.Contains("X-Cybozu-API-Token")) {
            this._httpClient.DefaultRequestHeaders.Add("X-Cybozu-API-Token", this._access.ApiToken);
        }
    }
    private Uri BuildRequestUri(string path, string? query = null) {
        if (string.IsNullOrWhiteSpace(this._access.Domain)) { throw new InvalidOperationException("Domain is not set."); }

        var baseUri = new Uri($"https://{this._access.Domain.TrimEnd('/')}/k/v1/");
        var builder = new UriBuilder(new Uri(baseUri, path));

        if (!string.IsNullOrEmpty(query)) { builder.Query = query; }

        return builder.Uri;
    }
    private void ApplyAuth(HttpRequestMessage request) {
        request.Headers.Add("X-Cybozu-API-Token", this._access.ApiToken);
    }
    private async Task<string> SendGetAsync(Uri uri) {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        this.ApplyAuth(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        this._logger?.LogTrace(json);

        if (!response.IsSuccessStatusCode) {
            var message = $"APIリクエストに失敗しました。StatusCode: {response.StatusCode}, Response: {json}";
            this._logger?.LogError(message);
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return json;
    }
    #region <<Protected methods>>
    /// <summary>
    /// HttpClientの初期化
    /// </summary>
    protected void InitHttpClient() {
        if (this._httpClient != null) { return; }

        this._httpClient = new HttpClient {
            BaseAddress = string.IsNullOrWhiteSpace(this._access.Domain) ? null : new Uri($"https://{this._access.Domain.TrimEnd('/')}/k/v1/"),
        };

        this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    /// <summary>
    /// BaseUri作成
    /// </summary>
    /// <returns>BaseUri</returns>
    /// <exception cref="InvalidOperationException">domain is not set.</exception>
    protected Uri GetBaseUri() {
        if (string.IsNullOrWhiteSpace(this._access.Domain)) { throw new InvalidOperationException("Domain is not set."); }
        return new Uri($"https://{this._access.Domain.TrimEnd('/')}/k/v1/");
    }
    /// <summary>
    /// AppID取得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>AppID</returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected int GetAppID<T>() where T : KintoneModelBase<T>, new() {
        var attr = typeof(T).GetCustomAttribute<KintoneItemAttribute>() ?? throw new InvalidOperationException($"KintoneItemAttribute is not defined on type {typeof(T).FullName}.");
        return this._appID;
    }
    /// <summary>
    /// リクエストヘッダ作成
    /// </summary>
    /// <param name="request"></param>
    protected void SetHeaders(HttpRequestMessage request) {
        this._access.ApplyAuthentication(request);
    }

    /// <summary>
    /// リソースを解放します
    /// </summary>
    public void Dispose() {
        this._httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion
}
