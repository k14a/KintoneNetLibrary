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
    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        Converters = {
            new KintoneRecordConverterFactory(),
        },
        // 必要に応じて他のオプションを追加
    };
    // /// <summary>
    // /// Kintoneデータ取得上限
    // /// </summary>
    // private const int KintoneLimit = 500;
    // /// <summary>
    // /// Kintoneデータ削除上限
    // /// </summary>
    // private const int KintoneDeleteLimit = 100;
    private readonly ILogger<KintoneApi>? _logger;
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
    #endregion

    #region <<Constructors>>
    /// <summary>
    /// Constructor
    /// </summary>
    public KintoneApi(ILogger<KintoneApi>? logger = null) {
        this.InitHttpClient();
        this._logger = logger;
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="domain">Kintoneドメイン</param>
    /// <param name="appID">Kintoneアプリケーション番号</param>
    /// <param name="logger"></param>
    public KintoneApi(string domain, int appID, ILogger<KintoneApi>? logger = null) {
        this.Domain = domain;
        this.AppID = appID;
        this._logger = logger;
        this.InitHttpClient();
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HttpClient</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appID">Kintoneアプリケーション番号</param>
    /// <param name="domain">Kintoneドメイン</param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException">httpClient is null / apiToken is null</exception>
    public KintoneApi(HttpClient httpClient, string apiToken, int appID, string domain = "", ILogger<KintoneApi>? logger = null) {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.ApiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));
        this.AppID = appID;
        this._logger = logger;
        this.Domain = domain;
        if (!string.IsNullOrWhiteSpace(domain)) {
            this._httpClient.BaseAddress = string.IsNullOrWhiteSpace(this.Domain) ? null : new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/");
        }
        this.EnsureDefaultHeaders();
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HttpClient</param>
    /// <param name="domain">Kintoneドメイン</param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException">httpClient is null</exception>
    public KintoneApi(HttpClient httpClient, string domain = "", ILogger<KintoneApi>? logger = null) {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this._logger = logger;
        this.Domain = domain;
        if (!string.IsNullOrWhiteSpace(domain)) {
            this._httpClient.BaseAddress = string.IsNullOrWhiteSpace(this.Domain) ? null : new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/");
        }
        this.EnsureDefaultHeaders();
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public KintoneApi(IOptions<KintoneApiOptions> options, ILogger<KintoneApi>? logger = null) {
        if (options?.Value == null) {
            throw new ArgumentNullException(nameof(options));
        }

        var value = options.Value;

        this.Domain = value.Domain;
        this.AppID = value.AppID;
        this.ApiToken = value.ApiToken;
        this._logger = logger;

        this.InitHttpClient();
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
