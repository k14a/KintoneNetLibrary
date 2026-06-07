using System.Text.Json;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api;
using Microsoft.Extensions.Logging;

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
    public IKintoneApi Create<T>(T model) where T : KintoneModelBase<T>, new() {
        return new KintoneApi(model.Access, model.AppId, this._httpClientFactory, this._logger);
    }

    /// <summary>
    /// アクセス情報とアプリ Id から KintoneApi のインスタンスを生成する
    /// </summary>
    /// <param name="access">Kintone アクセス情報</param>
    /// <param name="appId">アプリ Id</param>
    /// <param name="jsonOptions">JSON シリアライズオプション（省略時はデフォルト）</param>
    /// <returns>生成されたKintoneApiのインスタンス</returns>
    public IKintoneApi Create(KintoneAccessBase access, int appId, JsonSerializerOptions? jsonOptions = null) {
        return new KintoneApi(access, appId, this._httpClientFactory, this._logger, jsonOptions);
    }
}
