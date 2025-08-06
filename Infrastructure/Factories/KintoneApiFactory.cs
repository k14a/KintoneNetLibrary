using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Factories;

public class KintoneApiFactory : IKintoneApiFactory {
    private readonly HttpClient _httpClient;
    private readonly ILogger<KintoneApi> _logger;

    public KintoneApiFactory(HttpClient httpClient, ILogger<KintoneApi> logger) {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public KintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new() {
        return new KintoneApi(model.Access, model.AppID, _httpClient, _logger);
    }
}
