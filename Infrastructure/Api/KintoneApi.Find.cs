using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Infrastructure.Internal;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;

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
    public async Task<string> FindAllAsync<T>() where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>();
        return await FindBaseJsonAsync(query);
    }

    // フィールドと値で検索
    public async Task<string> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>().WhereEquals(field, value);
        return await FindBaseJsonAsync(query);
    }

    // 任意のkintoneクエリ文字列で検索
    public async Task<string> FindByQueryAsync<T>(string queryStr) where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>().SetQuery(queryStr);
        return await FindBaseJsonAsync(query);
    }

    // 内部的な共通検索処理
    private async Task<string> FindBaseJsonAsync<T>(KintoneQuery<T> query, bool skipThresholdCheck = false) where T : KintoneModelBase, new() {
        if (!skipThresholdCheck) {
            // 1) 件数取得
            var countUri = KintoneRequestBuilder.BuildRequestUri(this.GetBaseUri(), KintoneApiEndpoints.GetRecords, this.AppID, $"{query.Build(false)}&totalCount=true&limit=1");

            using var countReq = new HttpRequestMessage(HttpMethod.Get, countUri);
            this.SetHeaders(countReq);

            using var countResp = await this._httpClient.SendAsync(countReq);
            var countJson = await countResp.Content.ReadAsStringAsync();

            if (!countResp.IsSuccessStatusCode) {
                throw new KintoneException(KintoneErrorConverter.Parse(countJson));
            }

            var countResult = JsonSerializer.Deserialize<RecordCountResponse>(countJson, _jsonOptions) ?? new RecordCountResponse();

            if (countResult.TotalCount > KintoneLimit) {
                // カーソル API に切替
                return await this.CursorFetchAllJsonAsync<T>(query.Build(false));
            }
        }

        // 2) 通常取得
        var requestUri = KintoneRequestBuilder.BuildRequestUri(this.GetBaseUri(), KintoneApiEndpoints.GetRecords, this.AppID, $"{query.Build(true)}");

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
        var cursor = await this.CreateCursorJsonAsync(query);
        var resultJsonList = new List<string>();

        await foreach (var json in this.StreamCursorJsonAsync<T>(cursor)) {
            resultJsonList.Add(json);
        }

        // すべての JSON オブジェクトを配列形式でまとめる（必要に応じて調整可能）
        return $"[{string.Join(",", resultJsonList)}]";
    }

    /* ---------- 件数取得用 DTO ---------- */
    private sealed class RecordCountResponse {
        [JsonPropertyName("totalCount")]
        public string TotalCountRaw { get; set; } = string.Empty;
        [JsonIgnore()]
        public int TotalCount => Convert.ToInt32(this.TotalCountRaw);
    }
}
