using KintoneNetLibrary.Model;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace KintoneNetLibrary.Api;

public partial class KintoneApi {
    #region <<Private values>>
    private HttpClient _httpClient;
    // JsonSerializerOptions は再利用推奨のためstaticで保持
    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        // 必要に応じて他のオプションを追加
    };
    private const int KintoneLimit = 500;          // kintone 1 回取得上限
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
    public KintoneApi() { this.InitHttpClient(); }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="domain">Kintoneドメイン</param>
    /// <param name="appID">Kintoneアプリケーション番号</param>
    public KintoneApi(string domain, int appID) {
        this.Domain = domain;
        this.AppID = appID;
        this.InitHttpClient();
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HttpClient</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appID">Kintoneアプリケーション番号</param>
    /// <param name="domain">Kintoneドメイン</param>
    /// <exception cref="ArgumentNullException">httpClient is null / apiToken is null</exception>
    public KintoneApi(HttpClient httpClient, string apiToken, int appID, string domain = "") {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.ApiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));
        this.AppID = appID;
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
    /// <exception cref="ArgumentNullException">httpClient is null</exception>
    public KintoneApi(HttpClient httpClient, string domain = "") {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.Domain = domain;
        if (!string.IsNullOrWhiteSpace(domain)) {
            this._httpClient.BaseAddress = string.IsNullOrWhiteSpace(this.Domain) ? null : new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/");
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
