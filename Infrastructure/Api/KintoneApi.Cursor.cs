using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using KintoneNetLibrary.Infrastructure.Internal;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {

    /* ---------- カーソル作成 ---------- */
    public async Task<string> CreateCursorAsync(Dictionary<string, object> body) {
        using var request = new HttpRequestMessage(HttpMethod.Post, KintoneApiEndpoints.Cursor);
        request.Headers.Add("X-Cybozu-API-Token", this._access.ApiToken);
        request.Content = JsonContent.Create(body, options: this._jsonOptions);

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var created = JsonSerializer.Deserialize<CursorCreated>(json, this._jsonOptions);
        return created?.Id ?? throw new KintoneException("Cursor ID が取得できませんでした。");
    }

    /* ---------- 1ページ取得 ---------- */
    private async Task<string> FetchCursorAsync(string cursorId) {
        var endpoint = $"records/cursor.json?id={cursorId}";
        var requestUri = $"{this.GetBaseUri()}{endpoint}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        this._logger?.LogDebug("FetchCursorRawJson received: {Json}", json);

        if (!response.IsSuccessStatusCode) {
            var error = KintoneErrorConverter.Parse(json);
            this._logger?.LogError("FetchCursorRawJson failed: {Message}", error.Message);
            throw new KintoneException(error);
        }

        return json;
    }

    /* ---------- カーソル削除 ---------- */
    public async Task<string> DeleteCursorJsonAsync(string json) {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{this.GetBaseUri()}{KintoneApiEndpoints.Cursor}") {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        this.SetHeaders(request);

        var response = await this._httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /* ---------- 逐次ストリーム取得 ---------- */
    public async IAsyncEnumerable<string> StreamCursorAsync(string cursorId) {
        try {
            while (true) {
                var pageJson = await this.FetchCursorAsync(cursorId);

                using var doc = JsonDocument.Parse(pageJson);
                var hasNext = doc.RootElement.TryGetProperty("next", out var doneProp) && doneProp.GetBoolean();

                yield return pageJson;

                if (!hasNext) { break; }
            }

        } finally {
            var deleteRequestJson = JsonSerializer.Serialize(new { id = cursorId }, this._jsonOptions);
            try {
                await this.DeleteCursorJsonAsync(deleteRequestJson);
            } catch (KintoneException ex) when (ex.Detail.Contains("GAIA_CN01")) {
                // カーソルが自動終了されたため、エラーを握りつぶす
                this._logger?.LogWarning(ex.ToString());
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
