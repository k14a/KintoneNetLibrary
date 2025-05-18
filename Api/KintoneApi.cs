using KintoneNetLibrary.Types;
using KintoneNetLibrary.Model;
using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Api;

public partial class KintoneApi
{
    private string _domain = string.Empty;
    private string _apiToken = string.Empty;
    private string _user = string.Empty;
    private string _password = string.Empty;
    private HttpClient _httpClient = null;
    // JsonSerializerOptions は再利用推奨のためstaticで保持
    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        // 必要に応じて他のオプションを追加
    };
    private const int KintoneLimit = 500;          // kintone 1 回取得上限

    public string Domain { get; set; } = string.Empty;
    public int AppID { get; set; }
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public string ApiToken { get; set; }
    public string BaseUrl { get; set; }

    //public string User
    //{
    //    get => _user;
    //    set => _user = value;
    //}

    //public string Password
    //{
    //    get => _password;
    //    set => _password = value;
    //}

    //public string ApiURL => $"{_domain}k/v1/";
    //public int ReadLimit { get; set; } = 500;

    public KintoneApi() {
        this.InitHttpClient();
    }

    public KintoneApi(string domain, int appID) {
        this.Domain = domain;
        this.AppID = appID;
        this.InitHttpClient();
    }
    public KintoneApi(HttpClient httpClient, string domain = "") {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.Domain = domain;
        if (!string.IsNullOrWhiteSpace(domain)) {
            this._httpClient.BaseAddress = string.IsNullOrWhiteSpace(this.Domain) ? null : new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/");
        }
    }
    protected int GetAppID<T>() where T : KintoneModelBase, new() {
        var attr = typeof(T).GetCustomAttribute<KintoneItemAttribute>();
        if (attr == null) {
            throw new InvalidOperationException($"KintoneItemAttribute is not defined on type {typeof(T).FullName}.");
        }

        //return attr.AppID;
        return new T().AppID;
    }

    protected void InitHttpClient() {
        if (this._httpClient == null) { return; }

        this._httpClient = new HttpClient {
            BaseAddress = string.IsNullOrWhiteSpace(this.Domain) ? null : new Uri($"https://{this.Domain.TrimEnd('/')}/k/v1/"),
        };

        this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private Uri BuildRequestUri(string path, string query = null) {
        var baseUri = new Uri(BaseUrl); // BaseUrlはAPIのベースURL（例: https://example.kintone.com）
        var builder = new UriBuilder(new Uri(baseUri, path));
        if (!string.IsNullOrEmpty(query)) {
            builder.Query = query;
        }
        return builder.Uri;
    }

    private void SetHeaders(HttpRequestMessage request) {
        request.Headers.Add("X-Cybozu-API-Token", ApiToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // 他にも必要なヘッダーを設定
    }
}
