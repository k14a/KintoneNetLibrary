using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {

    /* ---------- カーソル作成 ---------- */
    // public async Task<string> CreateCursorJsonAsync(string body) {
    //     using var request = new HttpRequestMessage(HttpMethod.Post, "records/cursor.json");
    //     request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
    //     request.Content = JsonContent.Create(body, options: _jsonOptions);

    //     using var resp = await this._httpClient.SendAsync(request);
    //     var json = await resp.Content.ReadAsStringAsync();

    //     if (!resp.IsSuccessStatusCode) {
    //         throw new KintoneException(KintoneErrorConverter.Parse(json));
    //     }

    //     var created = JsonSerializer.Deserialize<CursorCreated>(json, _jsonOptions);
    //     return created?.Id ?? throw new KintoneException("Cursor ID が取得できませんでした。");
    // }
    public async Task<string> CreateCursorAsync(Dictionary<string, object> body) {
        using var request = new HttpRequestMessage(HttpMethod.Post, "records/cursor.json");
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        request.Content = JsonContent.Create(body, options: _jsonOptions);

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var created = JsonSerializer.Deserialize<CursorCreated>(json, _jsonOptions);
        return created?.Id ?? throw new KintoneException("Cursor ID が取得できませんでした。");
    }

    /* ---------- 1ページ取得 ---------- */
    // [Obsolete()]
    // public async Task<string> FetchCursorJsonAsync(string cursorId) {
    //     using var request = new HttpRequestMessage(HttpMethod.Get, $"records/cursor.json?id={cursorId}");
    //     request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

    //     using var response = await _httpClient.SendAsync(request);
    //     var json = await response.Content.ReadAsStringAsync();

    //     _logger?.LogDebug("FetchCursorJson received: {Json}", json);

    //     if (!response.IsSuccessStatusCode) {
    //         throw new KintoneException(KintoneErrorConverter.Parse(json));
    //     }

    //     return json;
    // }

    private async Task<string> FetchCursorAsync(string cursorId) {
        var endpoint = $"records/cursor.json?id={cursorId}";
        var requestUri = $"{GetBaseUri()}{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        SetHeaders(request);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("FetchCursorRawJson received: {Json}", json);

        if (!response.IsSuccessStatusCode) {
            var error = KintoneErrorConverter.Parse(json);
            _logger?.LogError("FetchCursorRawJson failed: {Message}", error.Message);
            throw new KintoneException(error);
        }

        return json;
    }
    // [Obsolete()]
    // public async Task<IList<string>> CursorFetchAllJsonAsync(string cursorId) {
    //     var allPages = new List<string>();

    //     while (true) {
    //         var json = await FetchCursorAsync(cursorId);
    //         allPages.Add(json);

    //         var parsed = JsonSerializer.Deserialize<JsonDocument>(json, _jsonOptions) ?? throw new KintoneException("FetchCursorAll: JSONのパースに失敗しました。");
    //         var next = parsed.RootElement.GetProperty("next").GetBoolean();
    //         if (!next) {
    //             break;
    //         }
    //     }

    //     return allPages;
    // }

    /* ---------- カーソル削除 ---------- */
    public async Task<string> DeleteCursorJsonAsync(string json) {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{GetBaseUri()}records/cursor.json") {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        SetHeaders(request);

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            // var error = KintoneErrorConverter.Parse(responseJson);
            // throw new KintoneException(error);
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /* ---------- 逐次ストリーム取得 ---------- */
    // [Obsolete()]
    // public async IAsyncEnumerable<T> StreamCursorAsync<T>(string query, IEnumerable<string>? fields = null, int size = CursorFetchLimit) where T : KintoneModelBase, new() {
    //     // カーソル作成用 JSON の構築
    //     var createRequest = new {
    //         app = new T().AppID,
    //         query,
    //         size,
    //         fields = fields ?? new List<string>()  // nullなら全フィールド
    //     };
    //     var createJson = JsonSerializer.Serialize(createRequest, _jsonOptions);
    //     var createResponseJson = await CreateCursorAsync(createJson);

    //     var createdCursor = JsonSerializer.Deserialize<CursorCreated>(createResponseJson, _jsonOptions) ?? throw new KintoneException("カーソル作成レスポンスの解析に失敗しました。");
    //     var cursorId = createdCursor.Id;

    //     try {
    //         while (true) {
    //             var fetchRequestJson = $"{{\"id\":\"{cursorId}\"}}";
    //             var fetchResponseJson = await FetchCursorJsonAsync(fetchRequestJson);

    //             var fetched = JsonSerializer.Deserialize<CursorFetch<T>>(fetchResponseJson, _jsonOptions) ?? new CursorFetch<T>();

    //             foreach (var record in fetched.Records) {
    //                 yield return record;
    //             }

    //             if (fetched.Done) {
    //                 break;
    //             }
    //         }
    //     } finally {
    //         var deleteRequestJson = JsonSerializer.Serialize(new { id = cursorId }, _jsonOptions);
    //         await DeleteCursorJsonAsync(deleteRequestJson);
    //     }
    // }

    // [Obsolete()]
    // public async IAsyncEnumerable<string> StreamCursorJsonAsync<T>(string query, IEnumerable<string>? fields = null, int size = CursorFetchLimit) where T : KintoneModelBase, new() {
    //     // カーソル作成リクエスト用オブジェクト
    //     var createRequest = new {
    //         app = new T().AppID,
    //         query,
    //         size,
    //         fields = fields ?? new List<string>()  // null → 全フィールド
    //     };

    //     // JSONに変換して送信
    //     var createJson = JsonSerializer.Serialize(createRequest, _jsonOptions);
    //     var createResponse = await CreateCursorAsync(createJson);

    //     // カーソルID取得
    //     var cursor = JsonSerializer.Deserialize<CursorCreated>(createResponse, _jsonOptions) ?? throw new KintoneException("カーソル作成に失敗しました。");
    //     var cursorId = cursor.Id;

    //     try {
    //         while (true) {
    //             // カーソル取得（JSON 文字列）
    //             var fetchRequestJson = $"{{\"id\":\"{cursorId}\"}}";
    //             var pageJson = await FetchCursorAsync(fetchRequestJson);

    //             yield return pageJson;

    //             using var doc = JsonDocument.Parse(pageJson);
    //             if (doc.RootElement.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean()) {
    //                 break;
    //             }
    //         }
    //     } finally {
    //         var deleteRequestJson = JsonSerializer.Serialize(new { id = cursorId }, _jsonOptions);
    //         await DeleteCursorJsonAsync(deleteRequestJson);
    //     }
    // }
    public async IAsyncEnumerable<string> StreamCursorAsync(string cursorId) {
        try {
            while (true) {
                var pageJson = await FetchCursorAsync(cursorId);

                using var doc = JsonDocument.Parse(pageJson);
                var hasNext = doc.RootElement.TryGetProperty("next", out var doneProp) && doneProp.GetBoolean();

                yield return pageJson;

                if (!hasNext) { break; }
            }

        } finally {
            var deleteRequestJson = JsonSerializer.Serialize(new { id = cursorId }, _jsonOptions);
            try {
                await DeleteCursorJsonAsync(deleteRequestJson);
            } catch (KintoneException ex) when (ex.Detail.Contains("GAIA_CN01")) {
                // カーソルが自動終了されたため、エラーを握りつぶす
                _logger?.LogWarning(ex.ToString());
            }
        }
    }

    /* =========================================================
       内部 DTO
       ========================================================= */
    private sealed class CursorCreated {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public sealed class CursorFetch<TRecord> {
        [JsonPropertyName("records")]
        public IList<TRecord> Records { get; set; } = new List<TRecord>();

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
