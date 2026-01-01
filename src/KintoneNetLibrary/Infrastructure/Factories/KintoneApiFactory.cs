using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Factories;

// コメントは日本語で記述
/// <summary>
/// KintoneApiのファクトリクラス
/// </summary>
/// <param name="httpClient"></param>
/// <param name="logger"></param>
public class KintoneApiFactory(HttpClient httpClient, ILogger<KintoneApi> logger) : IKintoneApiFactory {
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<KintoneApi> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// KintoneApiのインスタンスを生成する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <returns></returns>
    public KintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new() {
        return new KintoneApi(model.Access, model.AppID, this._httpClient, this._logger);
    }
}
