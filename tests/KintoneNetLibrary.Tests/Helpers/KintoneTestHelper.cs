using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Tests.Models;

namespace KintoneNetLibrary.Tests.Helpers;

public static class KintoneTestHelper {
    public static async Task<List<T>> CreateRecordsInChunksAsync<T>(KintoneApi api, IList<T> models) where T : class {
        var allCreated = new List<T>();
        foreach (var chunk in models.Chunk(KintoneConstants.KintoneLimit)) {
            var createJson = KintoneRequestBuilder.BuildCreateJson<BookModel>(chunk);
            var createResult = await api.CreateRecordsAsync(createJson);
            var parsed = KintoneResponseParser.ParseCreatedRecords<BookModel>(chunk.ToList(), createResult);
            allCreated.AddRange(parsed);
        }

        return allCreated;
    }

    public static async Task DeleteRecordsInChunksAsync<T>(KintoneApi api, IList<T> models) where T : class {
        foreach (var chunk in models.Chunk(KintoneConstants.KintoneLimit)) {
            var deleteJson = KintoneRequestBuilder.BuildDeleteJson(chunk);
            var deleteResult = await api.DeleteJsonAsync(deleteJson);
            if (deleteResult == null) {
                throw new InvalidOperationException("Delete failed on a chunk.");
            }
        }
    }
}
