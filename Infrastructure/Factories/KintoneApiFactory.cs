using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Factories;

public class KintoneApiFactory : IKintoneApiFactory {
    private readonly HttpClient _httpClient;
    private readonly ILogger<KintoneApi> _logger;

    public KintoneApiFactory(HttpClient httpclient, ILogger<KintoneApi> logger) {
        this._httpClient = httpclient ?? throw new ArgumentNullException(nameof(httpclient));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    // public KintoneApi Create(KintoneModelBase model) {
    //     var account = new KintoneAccount {
    //         Domain = model.Domain,
    //         ApiToken = model.ApiToken
    //     };

    //     return new KintoneApi(account, model.AppID, _httpClient, _logger);
    // }
    public KintoneApi Create(KintoneModelBase model) {
        var account = model.Account ?? throw new InvalidOperationException("KintoneAccount が未設定です");
        return new KintoneApi(account, model.AppID, _httpClient, _logger);
    }

    public KintoneApi Create(KintoneAccount account, int appID) {
        return new KintoneApi(account, appID, this._httpClient, this._logger);
    }
}
