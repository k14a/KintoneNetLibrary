using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Entities;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Factories;

/// <summary>
/// KintoneApiのファクトリクラス
/// </summary>
/// <param name="httpClientFactory">HTTPクライアントファクトリ</param>
/// <param name="logger">ロガー</param>
public class KintoneApiFactory(IHttpClientFactory httpClientFactory, ILogger<KintoneApi> logger) : IKintoneApiFactory {
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<KintoneApi> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// KintoneApiのインスタンスを生成する
    /// </summary>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <returns>生成されたKintoneApiのインスタンス</returns>
    public KintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new() {
        var client = this._httpClientFactory.CreateClient("Kintone");
        return new KintoneApi(model.Access, model.AppID, client, this._logger);
    }
}
