using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace KintoneNetLibrary.Infrastructure.Factories;

public class KintoneApiFactory : IKintoneApiFactory {
    private readonly HttpClient _httpClient;
    private readonly ILogger<KintoneApi> _logger;

    public KintoneApiFactory(HttpClient httpclient, ILogger<KintoneApi> logger) {
        this._httpClient = httpclient ?? throw new ArgumentNullException(nameof(httpclient));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public KintoneApi Create(KintoneAccount account, int appID) {
        return new KintoneApi(account, appID, this._httpClient, this._logger);
    }
}
