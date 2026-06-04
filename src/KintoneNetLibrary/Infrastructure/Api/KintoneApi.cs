using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Application.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Api;

/// <summary>
/// Kintone API 基底クラス
/// </summary>
#pragma warning disable CA1001 // 破棄可能なフィールドを所有する型は、破棄可能でなければなりません
public partial class KintoneApi : IKintoneApi {
#pragma warning restore CA1001 // 破棄可能なフィールドを所有する型は、破棄可能でなければなりません
    #region <<Private values>>
    private readonly KintoneAccessBase _access;
    private readonly int _appID;
    private HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger? _logger;
    private int _cursorPageSize = KintoneConstants.CursorFetchLimit;
    private long _maxUploadFileSize = KintoneConstants.MaxUploadFileSize;
    private int _maxUploadFileCount = KintoneConstants.MaxUploadFileCount;
    #endregion // <<Private values>>

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
    #endregion // <<Properties>>

    #region <<Constructor(s)>>
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="access">Kintoneへのアクセス情報を保持するオブジェクト</param>
    /// <param name="appID">KintoneアプリケーションID</param>
    /// <param name="httpClientFactory">HTTPクライアントファクトリ</param>
    /// <param name="logger">ロガー</param>
    /// <param name="jsonOptions">JSONシリアライズオプション</param>
    /// <exception cref="ArgumentNullException">accessがnullの場合にスローされます</exception>
    public KintoneApi(
        KintoneAccessBase access,
        int appID,
        IHttpClientFactory httpClientFactory,
        ILogger? logger = null,
        JsonSerializerOptions? jsonOptions = null) {

        ArgumentNullException.ThrowIfNull(access);

        this._access = access;
        this._appID = appID;
        this._logger = logger;
        this._httpClient = httpClientFactory.CreateClient("Kintone");
        this._jsonOptions = jsonOptions ?? DefaultJsonOptions.Default;

        this.EnsureDefaultHeaders();
    }
    #endregion // <<Constructor(s)>>

    /// <summary>
    /// HTTPクライアントのデフォルトヘッダを設定します
    /// </summary>
    private void EnsureDefaultHeaders() {
        if (!this._httpClient.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json")) {
            this._httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (!string.IsNullOrEmpty(this._access.ApiToken) && !this._httpClient.DefaultRequestHeaders.Contains("X-Cybozu-API-Token")) {
            this._httpClient.DefaultRequestHeaders.Add("X-Cybozu-API-Token", this._access.ApiToken);
        }
    }

    /// <summary>
    /// APIリクエスト用のURIを構築します
    /// </summary>
    /// <param name="path">APIのパス</param>
    /// <param name="query">クエリ文字列</param>
    /// <returns>構築されたURI</returns>
    /// <exception cref="InvalidOperationException">ドメインが設定されていない場合にスローされます</exception>
    private Uri BuildRequestUri(string path, string? query = null) {
        if (string.IsNullOrWhiteSpace(this._access.Domain)) { throw new InvalidOperationException("Domain is not set."); }

        var baseUri = new Uri($"https://{this._access.Domain.TrimEnd('/')}/k/v1/");
        var builder = new UriBuilder(new Uri(baseUri, path));

        if (!string.IsNullOrEmpty(query)) { builder.Query = query; }

        return builder.Uri;
    }

    /// <summary>
    /// APIリクエストに認証情報を適用します
    /// </summary>
    /// <param name="request">HTTPリクエストメッセージ</param>
    private void ApplyAuth(HttpRequestMessage request) {
        request.Headers.Add("X-Cybozu-API-Token", this._access.ApiToken);
    }

    /// <summary>
    /// GETリクエストを送信し、レスポンスのJSONを返します
    /// </summary>
    /// <param name="uri">リクエスト先のURI</param>
    /// <returns>レスポンスのJSON文字列</returns>
    /// <exception cref="KintoneException">APIリクエストが失敗した場合にスローされます</exception>
    private async Task<string> SendGetAsync(Uri uri) {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        this.ApplyAuth(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            if(this._logger != null) {
                _logErrorException(this._logger, $"API request failed. StatusCode: {response.StatusCode}, Response: {json}", null);
            }
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
    /// <returns>構築されたBaseUri</returns>
    /// <exception cref="InvalidOperationException">ドメインが設定されていない場合にスローされます</exception>
    protected Uri GetBaseUri() {
        if (string.IsNullOrWhiteSpace(this._access.Domain)) { throw new InvalidOperationException("Domain is not set."); }
        return new Uri($"https://{this._access.Domain.TrimEnd('/')}/k/v1/");
    }
    /// <summary>
    /// AppIDを取得します
    /// </summary>
    /// <typeparam name="T">Kintoneのレコードデータの型</typeparam>
    /// <returns>AppID</returns>
    /// <exception cref="InvalidOperationException">KintoneItemAttributeが定義されていない場合にスローされます</exception>
    protected int GetAppID<T>() where T : KintoneModelBase<T>, new() {
        var attr = typeof(T).GetCustomAttribute<KintoneItemAttribute>() ?? throw new InvalidOperationException($"KintoneItemAttribute is not defined on type {typeof(T).FullName}.");
        return this._appID;
    }
    /// <summary>
    /// リクエストヘッダ作成
    /// </summary>
    /// <param name="request">HTTPリクエストメッセージ</param>
    protected void SetHeaders(HttpRequestMessage request) {
        this._access.ApplyAuthentication(request);
    }
    #endregion // <<Protected methods>>
    
    #region <<Logging>>
    private static readonly Action<ILogger, string, Exception?> _logWarningException =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1001), "{Message}");
    private static readonly Action<ILogger, string, Exception?> _logErrorException =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1002), "{Message}");
    private static readonly Action<ILogger, string, Exception?> _logTraceException =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1003), "{Message}");
    private static readonly Action<ILogger, string, Exception?> _logDebugException =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1004), "{Message}");
    private static readonly Action<ILogger, string, Exception?> _logInformationException =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1005), "{Message}");
    #endregion // <<Logging>>
}
