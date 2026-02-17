using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KintoneNetLibrary.Application.UseCases.Services;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Factories;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Infrastructure.Repositories;
using KintoneNetLibrary.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// KintoneRequestBuilder クラスのユニットテスト。
/// </summary>
public static class KintoneTestHelper {
    /// <summary>
    /// テスト用の KintoneApi インスタンスを作成します。
    /// </summary>
    /// <returns></returns>
    public static KintoneApi CreateApi() {
        var cfg = TestEnv.Settings;
        var cli = new HttpClient {
            BaseAddress = new Uri($"https://{cfg.Domain}/k/v1/")
        };
        var access = new ApiTokenAccess(cfg.Domain, cfg.ApiToken);
        return new KintoneApi(access, cfg.AppID, cli);
    }

    /// <summary>
    /// 指定されたモデルのレコードをチャンクに分割して作成します
    /// </summary>
    /// <typeparam name="T">KintoneModelBase を継承したモデルの型</typeparam>
    /// <param name="api">KintoneApi インスタンス</param>
    /// <param name="models">作成するモデルのリスト</param>
    /// <returns>作成されたモデルのリスト</returns>
    public static async Task<IList<T>> CreateRecordsInChunksAsync<T>(KintoneApi api, IList<T> models) where T : KintoneModelBase<T>, new() {
        var allCreated = new List<T>();
        foreach (var chunk in models.Chunk(KintoneConstants.KintoneLimit)) {
            var createJson = KintoneRequestBuilder.BuildCreateJson(chunk);
            var createResult = await api.CreateAsync(createJson);
            var parsed = KintoneResponseParser.ParseCreatedRecords(chunk.ToList(), createResult);
            allCreated.AddRange(parsed);
        }

        return allCreated;
    }

    /// <summary>
    /// 指定されたモデルのレコードをチャンクに分割して削除します
    /// </summary>
    /// <typeparam name="T">KintoneModelBase を継承したモデルの型</typeparam>
    /// <param name="api">KintoneApi インスタンス</param>
    /// <param name="models">削除するモデルのリスト</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">削除に失敗した場合にスローされます</exception>
    public static async Task DeleteRecordsInChunksAsync<T>(KintoneApi api, IList<T> models) where T : KintoneModelBase<T>, new() {
        foreach (var chunk in models.Chunk(KintoneConstants.KintoneDeleteLimit)) {
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(chunk);
            var deleteResult = await api.DeleteAsync(deleteJson) ?? throw new InvalidOperationException("Delete failed on a chunk.");
        }
    }

    /// <summary>
    /// 指定されたクエリでレコード数が期待値に達するまで待機します
    /// </summary>
    /// <typeparam name="T">KintoneModelBase を継承したモデルの型</typeparam>
    /// <param name="api">KintoneApi インスタンス</param>
    /// <param name="query">検索クエリ</param>
    /// <param name="expectedCount">期待されるレコード数</param>
    /// <param name="maxRetry">最大リトライ回数</param>
    /// <param name="delayMilliseconds">リトライ間の待機時間（ミリ秒）</param>
    /// <returns>期待されるレコード数に達した場合、取得されたレコードのリスト</returns>
    /// <exception cref="TimeoutException">期待されるレコード数に達しなかった場合にスローされます</exception>
    public static async Task<IList<T>> WaitForExpectedRecordCountAsync<T>(KintoneApi api, string query, int expectedCount, int maxRetry = 6, int delayMilliseconds = 500) where T : KintoneModelBase<T>, new() {
        for (int retry = 0; retry < maxRetry; retry++) {
            var foundJson = await api.FindByQueryAsync<T>(query);
            var foundRecords = KintoneResponseParser.ParseRecords<T>(foundJson);

            if (foundRecords != null && foundRecords.Count == expectedCount) {
                return foundRecords;
            }

            await Task.Delay(delayMilliseconds);
        }

        throw new TimeoutException($"Expected {expectedCount} records, but condition was not met after {maxRetry} retries.");
    }

    /// <summary>
    /// KintoneTypedCrudServiceインスタンスを作成します
    /// </summary>
    /// <typeparam name="T">KintoneModelBase を継承したモデルの型</typeparam>
    /// <returns>作成された KintoneTypedCrudService インスタンス</returns>
    public static KintoneTypedCrudService<T> CreateCrudService<T>() where T : KintoneModelBase<T>, new() {
        var config = TestEnv.Settings;
        var options = new KintoneExecutionOptions { MaxConcurrency = 2 };

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddHttpClient("Kintone", client => {
            client.BaseAddress = new Uri($"https://{config.Domain}/k/v1/");
        });

        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        var httpClient = httpClientFactory.CreateClient("Kintone");
        var apiLogger = loggerFactory.CreateLogger<KintoneApi>();
        var factory = new KintoneApiFactory(httpClient, apiLogger);
        var repository = new KintoneRepository(factory);

        var serviceLogger = loggerFactory.CreateLogger<KintoneTypedCrudService<T>>();
        return new KintoneTypedCrudService<T>(
            repository,
            Options.Create(options),
            new JsonSerializerOptions(),
            serviceLogger
        );
    }
}
