using System.Net.Http.Headers;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Infrastructure.Api;

public abstract class BaseKintoneApi
{
    protected readonly KintoneAccessBase Access;
    protected readonly HttpClient HttpClient;
    protected readonly ILogger? Logger;

    protected BaseKintoneApi( KintoneAccessBase access, HttpClient httpClient, ILogger? logger = null) {
        this.Access = access;
        this.HttpClient = httpClient;
        this.Logger = logger;

        this.EnsureDefaultHeaders();
    }

    protected void EnsureDefaultHeaders() {
        if (!this.HttpClient.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json")) {
            this.HttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (!string.IsNullOrEmpty(this.Access.ApiToken) && !this.HttpClient.DefaultRequestHeaders.Contains("X-Cybozu-API-Token")) {
            this.HttpClient.DefaultRequestHeaders.Add("X-Cybozu-API-Token", this.Access.ApiToken);
        }
    }

    protected Uri BuildRequestUri(string path, string? query = null) {
        if (string.IsNullOrWhiteSpace(this.Access.Domain)) { throw new InvalidOperationException("Domain is not set."); }

        var baseUri = new Uri($"https://{this.Access.Domain.TrimEnd('/')}/k/v1/");
        var builder = new UriBuilder(new Uri(baseUri, path));

        if (!string.IsNullOrEmpty(query)) { builder.Query = query; }

        return builder.Uri;
    }

    protected void ApplyAuth(HttpRequestMessage request) {
        this.Access.ApplyAuthentication(request);
    }


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
