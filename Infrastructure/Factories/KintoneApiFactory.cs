using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Factories;

public class KintoneApiFactory : IKintoneApiFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public KintoneApiFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public KintoneApi CreateFromModel(KintoneModelBase model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(model.Domain)) {
            throw new InvalidOperationException("モデルの Domain が未設定です。");
        }

        return Create(model.Domain, model.ApiToken);
    }

    public KintoneApi Create(string domain, string apiToken)
    {
        if (string.IsNullOrWhiteSpace(domain)) {
            throw new ArgumentException("Domain は必須です。", nameof(domain));
        }

        var httpClient = _httpClientFactory.CreateClient(); // 名前付きクライアントで分離してもよい
        httpClient.BaseAddress = new Uri($"https://{domain.TrimEnd('/')}/k/v1/");

        return new KintoneApi(httpClient, apiToken);
    }
}
