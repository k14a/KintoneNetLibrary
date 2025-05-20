using KintoneNetLibrary.Model;
using KintoneNetLibrary.Types;
using KintoneNetLibrary.Internal;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Api;

public partial class KintoneApi {
    public async Task<T?> FindByIDAsync<T>(string id) where T : KintoneModelBase, new() {
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

        var wrapper = JsonSerializer.Deserialize<KintoneRecordResponse<T>>(json, _jsonOptions);
        return wrapper?.Record;
    }
    // IDリストで複数レコードを取得
    public async Task<IList<T>> FindByIDsAsync<T>(IList<string> ids) where T : KintoneModelBase, new() {
        if (ids == null || ids.Count == 0) {
            throw new ArgumentNullException(nameof(ids));
        }

        if (ids.Count <= KintoneLimit) {
            // クエリ文字列を直接生成
            // var idConditions = string.Join(" or ", ids.Select(x => $"$id=\"{x}\""));
            var query = new KintoneQuery<T>().WhereIdIn(ids);
            var appID = new T().AppID;
            var requestUri = this.BuildRequestUri(KintoneApiEndpoints.GetRecords, $"app={appID}&{query}");

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            this.SetHeaders(request);

            using var response = await this._httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                throw new KintoneException(KintoneErrorConverter.Parse(json));
            }

            var wrapper = JsonSerializer.Deserialize<KintoneRecordsResponse<T>>(json, _jsonOptions) ?? new KintoneRecordsResponse<T>();
            return wrapper?.Records;

        } else {
            var query = new KintoneQuery<T>().WhereIdIn(ids);
            return await FindBaseAsync(query);
        }
    }

    // 全レコード取得（条件なし）
    public async Task<IList<T>> FindAllAsync<T>() where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>();
        return await FindBaseAsync(query);
    }

    // フィールドと値で検索
    public async Task<IList<T>> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>().WhereEquals(field, value);
        return await FindBaseAsync(query);
    }

    // 任意のkintoneクエリ文字列で検索
    public async Task<IList<T>> FindByQueryAsync<T>(string queryStr) where T : KintoneModelBase, new() {
        var query = new KintoneQuery<T>().SetQuery(queryStr);
        return await FindBaseAsync(query);
    }

    // 内部的な共通検索処理
    private async Task<IList<T>> FindBaseAsync<T>(KintoneQuery<T> query) where T : KintoneModelBase, new() {
        // 1) まず件数だけ取得 -------------------------------------------
        var countUri = KintoneRequestBuilder.BuildRequestUri(this.GetBaseUri(), KintoneApiEndpoints.GetRecords, this.AppID, $"{query.Build(false)}&totalCount=true&limit=1");

        using var countReq = new HttpRequestMessage(HttpMethod.Get, countUri);
        this.SetHeaders(countReq);

        using var countResp = await this._httpClient.SendAsync(countReq);
        var countJson = await countResp.Content.ReadAsStringAsync();

        if (!countResp.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(countJson));
        }

        var countResult = JsonSerializer.Deserialize<RecordCountResponse>(countJson, _jsonOptions) ?? new RecordCountResponse();

        // 2) しきい値判定 --------------------------------------------------
        if (countResult.TotalCount > KintoneLimit) {
            // カーソル API に切替
            return await this.CursorFetchAllAsync<T>(query.Build(false));
        }

        // 3) 従来の単発取得 ------------------------------------------------
        var requestUri = KintoneRequestBuilder.BuildRequestUri(this.GetBaseUri(), KintoneApiEndpoints.GetRecords, this.AppID, $"{query.Build(false)}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var recordsWrapper = JsonSerializer.Deserialize<KintoneRecords<T>>(json, _jsonOptions) ?? new KintoneRecords<T>();

        return recordsWrapper.Records ?? [];
    }

    /* ---------- カーソル API を使って最後まで取得 ---------- */
    private async Task<IList<T>> CursorFetchAllAsync<T>(string query) where T : KintoneModelBase, new() {
        var list = new List<T>();

        await foreach (var rec in this.StreamCursorAsync<T>(query)) {
            list.Add(rec);
        }
        return list;
    }
    /* ---------- 件数取得用 DTO ---------- */
    private sealed class RecordCountResponse {
        [JsonPropertyName("totalCount")]
        public string TotalCountRaw { get; set; } = string.Empty;
        [JsonIgnore()]
        public int TotalCount => Convert.ToInt32(this.TotalCountRaw);
    }
}
