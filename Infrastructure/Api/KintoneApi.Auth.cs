using System.Net.Http.Headers;
using System.Text;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi
{
    private string _basicAuthUser = string.Empty;
    private string _basicAuthPassword = string.Empty;
    private string _subdomain = string.Empty;

    public void SetDomain(string subdomain) {
        this._subdomain = subdomain;
        this._httpClient.BaseAddress = new Uri($"https://{_subdomain}.cybozu.com/k/v1/");
    }

    public void SetApiToken(string apiToken) {
        this.ApiToken = apiToken;
        ClearAuthorization();
        this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);
    }

    public void SetBasicAuth(string username, string password) {
        this._basicAuthUser = username;
        this._basicAuthPassword = password;
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_basicAuthUser}:{_basicAuthPassword}"));
        this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
    }

    public void ClearAuthorization() {
        this._httpClient.DefaultRequestHeaders.Authorization = null;
    }
}
