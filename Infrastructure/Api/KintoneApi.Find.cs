using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Infrastructure.Internal;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using KintoneNetLibrary.Domain.Common;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {
    public async Task<string?> FindByIDAsync<T>(string id) where T : KintoneModelBase, new() {
        if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentNullException(nameof(id)); }

        var appID = new T().AppID;
        var requestUri = this.BuildRequestUri(KintoneApiEndpoints.GetSingleRecord, $"app={appID}&id={id}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        this._logger?.LogTrace(json);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return json;
    }
    // IDリストで複数レコードを取得
    public async Task<string?> FindByIDsAsync<T>(IList<string> ids) where T : KintoneModelBase, new() {
        if (ids == null || ids.Count == 0) {
            throw new ArgumentNullException(nameof(ids));
        }

        if (ids.Count <= KintoneLimit) {
            var query = new KintoneQuery<T>().WhereIdsEquals(ids);
            var appID = new T().AppID;
            var requestUri = this.BuildRequestUri(KintoneApiEndpoints.GetRecords, $"app={appID}&query={query}");

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            this.SetHeaders(request);

            using var response = await this._httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                throw new KintoneException(KintoneErrorConverter.Parse(json));
            }

            return json;

        } else {
            var query = new KintoneQuery<T>().WhereIdIn(ids);
            return await FindBaseJsonAsync(query, skipThresholdCheck: true);
        }
    }

    // 全レコード取得（条件なし）
    public async Task<string?> FindAllAsync<T>() where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>();
        return await FindBaseJsonAsync(query);
    }

    // フィールドと値で検索
    public async Task<string?> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>().WhereEquals(field, value);
        return await FindBaseJsonAsync(query);
    }

    // 任意のkintoneクエリ文字列で検索
    public async Task<string?> FindByQueryAsync<T>(string queryStr) where T : KintoneModelBase, new() {
        KintoneQueryValidator.ValidateLikeClause(queryStr, msg => _logger?.LogWarning(msg));
        var query = new KintoneQuery<T>().SetQuery(queryStr);
        return await FindBaseJsonAsync(query);
    }

    // 内部的な共通検索処理
    private async Task<string?> FindBaseJsonAsync<T>(KintoneQuery<T> query, bool skipThresholdCheck = false) where T : KintoneModelBase, new() {
        if (!skipThresholdCheck) {
            // 1) 件数取得（limit=1 で totalCount を得る）
            var queryText = query.Build();
            var countUri = KintoneRequestBuilder.BuildRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this.AppID,
                queryText,
                new Dictionary<string, string> {
                    { "totalCount", "true" },
                    { "limit", "1" },
                });

            using var countReq = new HttpRequestMessage(HttpMethod.Get, countUri);
            this.SetHeaders(countReq);

            using var countResp = await this._httpClient.SendAsync(countReq);
            var countJson = await countResp.Content.ReadAsStringAsync();

            if (!countResp.IsSuccessStatusCode) {
                throw new KintoneException(KintoneErrorConverter.Parse(countJson));
            }

            var countResult = JsonSerializer.Deserialize<RecordCountResponse>(countJson, _jsonOptions) ?? new RecordCountResponse();

            // 実データ件数がKintoneの制限（通常100件）を超える場合はカーソル API に切り替える
            if (countResult.TotalCount > KintoneLimit) {
                return await this.CursorFetchAllJsonAsync<T>(query.Build());
            }
        }

        // 2) 通常取得（最大100件まで）
        var requestUri = KintoneRequestBuilder.BuildRequestUri(
            this.GetBaseUri(),
            KintoneApiEndpoints.GetRecords,
            this.AppID,
            query.Build());

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return json;
    }

    /* ---------- カーソル API を使って最後まで取得 ---------- */
    private async Task<string> CursorFetchAllJsonAsync<T>(string query) where T : KintoneModelBase, new() {
        var cursorRequest = new Dictionary<string, object> {
            ["app"] = this.AppID,
            ["fields"] = typeof(T).GetKintoneFieldCodes(),
            ["size"] = this.CursorPageSize,
        };
        if (!string.IsNullOrEmpty(query)) {
            cursorRequest["query"] = query;
        }

        var cursor = await this.CreateCursorAsync(cursorRequest);
        var allRecords = new List<JsonElement>();

        await foreach (var json in this.StreamCursorAsync(cursor)) {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("records", out var recordsElement)) {
                foreach (var record in recordsElement.EnumerateArray()) {
                    allRecords.Add(record.Clone());
                }
            }
        }

        return JsonSerializer.Serialize(new { records = allRecords }, _jsonOptions);
    }

    /* ---------- 件数取得用 DTO ---------- */
    private sealed class RecordCountResponse {
        [JsonPropertyName("totalCount")]
        public string TotalCountRaw { get; set; } = string.Empty;
        [JsonIgnore()]
        public int TotalCount => Convert.ToInt32(this.TotalCountRaw);
    }
}
