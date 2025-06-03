using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {
    #region <<Private values>>
    private HttpClient _httpClient;
    // JsonSerializerOptions は再利用推奨のためstaticで保持
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<KintoneApi>? _logger;
    private int _cursorPageSize = CursorFetchLimit;
    #endregion

    #region <<Properties>>
    /// <summary>
    /// Kintoneドメイン
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    /// <summary>
    /// Kintoneアプリケーション番号
    /// </summary>
    public int AppID { get; set; }
    /// <summary>
    /// エンコード
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    /// <summary>
    /// ApiToken
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;
    /// <summary>
    /// Kintoneログイン名
    /// </summary>
    public string LoginName { get; set; } = string.Empty;
    /// <summary>
    /// Kintoneログインパスワード
    /// </summary>
    public string Password { get; set; } = string.Empty;
    /// <summary>
    /// Basic認証ユーザ名
    /// </summary>
    public string BasicAuthUser { get; set; } = string.Empty;
    /// <summary>
    /// Basic認証パスワード
    /// </summary>
    public string BasicAuthPassword { get; set; } = string.Empty;
    /// <summary>
    /// カーソルAPIで一度に取得する件数(省略時はKintoneの最大値である500)
    /// </summary>
    public int CursorPageSize {
        get => this._cursorPageSize;
        set {
            if (value <= 0 || value > CursorFetchLimit) {
                throw new ArgumentOutOfRangeException(nameof(this.CursorPageSize), value, $"CursorPageSizeは1以上{CursorFetchLimit}以下でなければなりません。");
            }
            this._cursorPageSize = value;
        }
    }
    #endregion

    #region <<Constructor(s)>>
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="account">KintoneAccount</param>
    /// <param name="appID">KintoneアプリケーションID</param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    /// <param name="jsonOptions"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public KintoneApi(KintoneAccount account, int appID, HttpClient? httpClient = null, ILogger<KintoneApi>? logger = null, JsonSerializerOptions? jsonOptions = null) {
        ArgumentNullException.ThrowIfNull(account);

        this.Domain = account.Domain;
        this.AppID = appID;
        this._logger = logger;
        this._httpClient = httpClient ?? new HttpClient();
        this._jsonOptions = jsonOptions ?? DefaultJsonOptions.Default;

        if (!string.IsNullOrWhiteSpace(this.Domain)) {
            this._httpClient.BaseAddress = new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/");
        }

        // 優先順位：APIトークン → ログイン名/パスワード → BasicAuth
        if (!string.IsNullOrWhiteSpace(account.ApiToken)) {
            this.ApiToken = account.ApiToken;
        } else if (!string.IsNullOrWhiteSpace(account.LoginName) && !string.IsNullOrWhiteSpace(account.Password)) {
            this.LoginName = account.LoginName;
            this.Password = account.Password;
        }

        if (!string.IsNullOrWhiteSpace(account.BasicAuthUser) && !string.IsNullOrWhiteSpace(account.BasicAuthPassword)) {
            this.BasicAuthUser = account.BasicAuthUser;
            this.BasicAuthPassword = account.BasicAuthPassword;
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
            BaseAddress = string.IsNullOrWhiteSpace(this.Domain) ? null : new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/"),
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

        if (!string.IsNullOrEmpty(this.ApiToken) && !this._httpClient.DefaultRequestHeaders.Contains("X-Cybozu-API-Token")) {
            this._httpClient.DefaultRequestHeaders.Add("X-Cybozu-API-Token", this.ApiToken);
        }
    }
    /// <summary>
    /// BaseUri作成
    /// </summary>
    /// <returns>BaseUri</returns>
    /// <exception cref="InvalidOperationException">domain is not set.</exception>
    protected Uri GetBaseUri() {
        if (string.IsNullOrWhiteSpace(this.Domain)) { throw new InvalidOperationException("Domain is not set."); }
        return new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/");
    }
    /// <summary>
    /// AppID取得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>AppID</returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected int GetAppID<T>() where T : KintoneModelBase, new() {
        var attr = typeof(T).GetCustomAttribute<KintoneItemAttribute>() ?? throw new InvalidOperationException($"KintoneItemAttribute is not defined on type {typeof(T).FullName}.");
        return new T().AppID;
    }
    /// <summary>
    /// 
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
    /// 
    /// </summary>
    /// <param name="request"></param>
    protected void SetHeaders(HttpRequestMessage request) {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(this.ApiToken)) {
            request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        }
        // 他にも必要なヘッダーを設定
    }
    #endregion
}
