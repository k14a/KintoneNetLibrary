using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {
    /* =========================================================
       Cursor API
       ---------------------------------------------------------
       POST   /records/cursor.json   … CreateCursorAsync
       GET    /records/cursor.json   … FetchCursorAsync
       DELETE /records/cursor.json   … DeleteCursorAsync
       ========================================================= */

    private const int CursorDefaultSize = 500;

    /* ---------- カーソル作成 ---------- */
    public async Task<string> CreateCursorAsync<T>(string query, IEnumerable<string>? fields = null, int size = CursorDefaultSize) where T : KintoneModelBase, new() {
        var model = new T();

        var body = new {
            app = model.AppID,
            query = query,
            size = size,
            fields = fields ?? new List<string>()  // null→全フィールド
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "records/cursor.json");
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        request.Content = JsonContent.Create(body, options: _jsonOptions);

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var created = JsonSerializer.Deserialize<CursorCreated>(json, _jsonOptions);
        return created?.Id
            ?? throw new KintoneException("Cursor ID が取得できませんでした。");
    }

    /* ---------- 1ページ取得 ---------- */
    public async Task<CursorFetch<T>> FetchCursorAsync<T>(string cursorId) {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"records/cursor.json?id={cursorId}");
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return JsonSerializer.Deserialize<CursorFetch<T>>(json, _jsonOptions)
               ?? new CursorFetch<T>();
    }
    private async Task<string> FetchCursorRawJsonAsync(string cursorId) {
        var requestUri = $"{this.GetBaseUri()}/k/v1/records/cursor.json?id={cursorId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return json;
    }


    /* ---------- カーソル削除 ---------- */
    public async Task DeleteCursorAsync(string cursorId) {
        var body = new { id = cursorId };

        using var request = new HttpRequestMessage(HttpMethod.Delete, "records/cursor.json") {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }
    }

    /* ---------- 逐次ストリーム取得 ---------- */
    public async IAsyncEnumerable<T> StreamCursorAsync<T>(
        string query,
        IEnumerable<string>? fields = null,
        int size = CursorDefaultSize
    ) where T : KintoneModelBase, new() {
        var cursorId = await this.CreateCursorAsync<T>(query, fields, size);

        try {
            while (true) {
                var page = await this.FetchCursorAsync<T>(cursorId);
                foreach (var record in page.Records) {
                    yield return record;
                }

                if (page.Done) {
                    break;
                }
            }
        } finally {
            await this.DeleteCursorAsync(cursorId);
        }
    }
    public async IAsyncEnumerable<string> StreamCursorJsonAsync<T>(string query, IEnumerable<string>? fields = null, int size = CursorDefaultSize) where T : KintoneModelBase, new() {
        var cursorId = await this.CreateCursorAsync<T>(query, fields, size);

        try {
            while (true) {
                var page = await this.FetchCursorRawJsonAsync(cursorId); // JSON 文字列としてページを取得
                yield return page;

                using var doc = JsonDocument.Parse(page);
                if (doc.RootElement.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean()) {
                    break;
                }
            }
        } finally {
            await this.DeleteCursorAsync(cursorId);
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
