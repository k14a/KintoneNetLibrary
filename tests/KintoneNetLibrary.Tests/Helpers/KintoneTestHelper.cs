using System.Security.Cryptography;
using System.Text;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Tests.Models;

namespace KintoneNetLibrary.Tests.Helpers;

public static class KintoneTestHelper {
    public static KintoneApi CreateApi() {
        var cfg = TestEnv.Settings;
        var cli = new HttpClient {
            BaseAddress = new Uri($"https://{cfg.Domain}/k/v1/")
        };
        var account = new KintoneAccount { ApiToken = cfg.ApiToken, Domain = cfg.Domain };
        return new KintoneApi(account, cfg.AppID, cli);
    }

    public static async Task<IList<T>> CreateRecordsInChunksAsync<T>(KintoneApi api, IList<T> models) where T : KintoneModelBase, new() {
        var allCreated = new List<T>();
        foreach (var chunk in models.Chunk(KintoneConstants.KintoneLimit)) {
            var createJson = KintoneRequestBuilder.BuildCreateJson(chunk);
            var createResult = await api.CreateRecordsAsync(createJson);
            var parsed = KintoneResponseParser.ParseCreatedRecords(chunk.ToList(), createResult);
            allCreated.AddRange(parsed);
        }

        return allCreated;
    }

    public static async Task DeleteRecordsInChunksAsync<T>(KintoneApi api, IList<T> models) where T : KintoneModelBase {
        foreach (var chunk in models.Chunk(KintoneConstants.KintoneDeleteLimit)) {
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(chunk);
            var deleteResult = await api.DeleteJsonAsync(deleteJson) ?? throw new InvalidOperationException("Delete failed on a chunk.");
        }
    }

    public static async Task<IList<T>> WaitForExpectedRecordCountAsync<T>(KintoneApi api, string query, int expectedCount, int maxRetry = 6, int delayMilliseconds = 500) where T : KintoneModelBase, new() {
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

}
