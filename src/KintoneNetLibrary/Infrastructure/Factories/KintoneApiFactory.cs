using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Factories;

/// <summary>
/// KintoneApiのファクトリクラス
/// </summary>
/// <param name="httpClient">HTTPクライアント</param>
/// <param name="logger">ロガー</param>
public class KintoneApiFactory(HttpClient httpClient, ILogger<KintoneApi> logger) : IKintoneApiFactory {
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<KintoneApi> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// KintoneApiのインスタンスを生成する
    /// </summary>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <returns>生成されたKintoneApiのインスタンス</returns>
    public KintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new() {
        return new KintoneApi(model.Access, model.AppID, this._httpClient, this._logger);
    }
}
