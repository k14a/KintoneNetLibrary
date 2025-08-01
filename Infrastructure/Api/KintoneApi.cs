using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {
    #region <<Private values>>
    private readonly KintoneAccessBase _access;
    private readonly int _appID;
    private HttpClient _httpClient;
    // JsonSerializerOptions は再利用推奨のためstaticで保持
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
    public KintoneApi(KintoneAccessBase access, int appID, HttpClient? httpClient = null, ILogger<KintoneApi>? logger = null, JsonSerializerOptions? jsonOptions = null) {
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
    /// HttpClientヘッダ作成
    /// </summary>
    protected void EnsureDefaultHeaders() {
        if (!this._httpClient.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json")) {
            this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (!string.IsNullOrEmpty(this._access.ApiToken) && !this._httpClient.DefaultRequestHeaders.Contains("X-Cybozu-API-Token")) {
            this._httpClient.DefaultRequestHeaders.Add("X-Cybozu-API-Token", this._access.ApiToken);
        }
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
    protected int GetAppID<T>() where T : KintoneModelBase, new() {
        var attr = typeof(T).GetCustomAttribute<KintoneItemAttribute>() ?? throw new InvalidOperationException($"KintoneItemAttribute is not defined on type {typeof(T).FullName}.");
        // return new T().AppID;
        return this._appID;
    }
    /// <summary>
    /// リクエストURL作成
    /// </summary>
    /// <param name="path"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    private Uri BuildRequestUri(string path, string? query = null) {
        var baseUri = this.GetBaseUri();
        var builder = new UriBuilder(new Uri(baseUri, path));

        if (!string.IsNullOrEmpty(query)) {
            builder.Query = query;
        }
        return builder.Uri;
    }
    /// <summary>
    /// リクエストヘッダ作成
    /// </summary>
    /// <param name="request"></param>
    protected void SetHeaders(HttpRequestMessage request) {
        this._access.ApplyAuthentication(request);
    }
    #endregion
}
