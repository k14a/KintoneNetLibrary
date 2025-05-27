using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Infrastructure.Factories;

public class KintoneApiFactory : IKintoneApiFactory {
    // private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KintoneApi> _logger;

    public KintoneApiFactory(Httpclient httpclient, ILogger<KintoneApi> logger) {
        this._httpClient = httpclient ?? throw new ArgumentNullException(nameof(httpclient));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public KintoneApi Create(KintoneAccount account, int appID) {
        return new KintoneApi(account, appID, this._httpClient, this._logger);
    }

    // public KintoneApiFactory(IHttpClientFactory httpClientFactory)
    // {
    //     _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    // }

    // public KintoneApi CreateFromModel(KintoneModelBase model)
    // {
    //     if (model == null) throw new ArgumentNullException(nameof(model));
    //     if (string.IsNullOrWhiteSpace(model.Domain)) {
    //         throw new InvalidOperationException("モデルの Domain が未設定です。");
    //     }

    //     return Create(model.Domain, model.ApiToken);
    // }

    // public KintoneApi Create(string domain, string apiToken)
    // {
    //     if (string.IsNullOrWhiteSpace(domain)) {
    //         throw new ArgumentException("Domain は必須です。", nameof(domain));
    //     }

    //     var httpClient = _httpClientFactory.CreateClient(); // 名前付きクライアントで分離してもよい
    //     httpClient.BaseAddress = new Uri($"https://{domain.TrimEnd('/')}/k/v1/");

    //     return new KintoneApi(httpClient, apiToken);
    // }
}
