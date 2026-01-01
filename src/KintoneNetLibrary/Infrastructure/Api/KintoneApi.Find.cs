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
using KintoneNetLibrary.Application.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Api;

/// <summary>
/// Kintone API - レコード取得
/// </summary>
public partial class KintoneApi : BaseKintoneApi, IKintoneApi {
    /// <summary>
    /// IDで単一レコードを取得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<string?> FindByIDAsync<T>(string id) where T : KintoneModelBase<T>, new() {
        if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentNullException(nameof(id)); }

        // var appID = new T().AppID;
        var requestUri = this.BuildRequestUri(KintoneApiEndpoints.GetSingleRecord, $"app={this._appID}&id={id}");

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
    /// <summary>
    /// IDリストで複数レコードを取得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ids"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<string?> FindByIDsAsync<T>(IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        if (ids == null || ids.Count == 0) {
            throw new ArgumentNullException(nameof(ids));
        }

        if (ids.Count <= KintoneLimit) {
            var query = new KintoneQuery<T>().WhereIdsEquals(ids);
            var requestUri = KintoneRequestBuilder.BuildFindRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this._appID,
                query.Build(),
                fieldCodes
            );

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
            return await this.FindBaseJsonAsync(query, forceCursor: true);
        }
    }

    /// <summary>
    /// 全レコード取得（条件なし）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    public async Task<string?> FindAllAsync<T>(IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        var query = new KintoneQuery<T>();
        return await this.FindBaseJsonAsync(query, fieldCodes: fieldCodes);
    }

    /// <summary>
    /// 指定フィールド＝値 で検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<string?> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase<T>, new() {
        var query = new KintoneQuery<T>().WhereEquals(field, value);
        return await this.FindBaseJsonAsync(query);
    }

    /// <summary>
    /// クエリ文字列で検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryStr"></param>
    /// <returns></returns>
    public async Task<string?> FindByQueryAsync<T>(string queryStr) where T : KintoneModelBase<T>, new() {
        KintoneQueryValidator.ValidateLikeClause(queryStr, msg => this._logger?.LogWarning(msg));
        var query = new KintoneQuery<T>().SetQuery(queryStr);
        return await this.FindBaseJsonAsync(query);
    }

    /// <summary>
    /// 基本検索処理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="fieldCodes"></param>
    /// <param name="forceCursor"></param>
    /// <returns></returns>
    /// <exception cref="KintoneException"></exception>
    private async Task<string?> FindBaseJsonAsync<T>(KintoneQuery<T> query, IList<string>? fieldCodes = null, bool forceCursor = false) where T : KintoneModelBase<T>, new() {
        if (!forceCursor) {
            // 1) 件数取得（limit=1 で totalCount を得る）
            var queryText = query.Build();
            var countUri = KintoneRequestBuilder.BuildRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this._appID,
                queryText,
                fieldCodes,
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

            var countResult = JsonSerializer.Deserialize<RecordCountResponse>(countJson, this._jsonOptions) ?? new RecordCountResponse();

            // 実データ件数がKintoneの制限（通常100件）を超える場合はカーソル API に切り替える
            if (countResult.TotalCount > KintoneLimit) {
                return await this.CursorFetchAllJsonAsync<T>(query.Build());
            }
        }

        // 2) 通常取得（最大100件まで）
        var requestUri = KintoneRequestBuilder.BuildRequestUri(
            this.GetBaseUri(),
            KintoneApiEndpoints.GetRecords,
            this._appID,
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

    /// <summary>
    /// カーソルで全件取得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <returns></returns>
    private async Task<string> CursorFetchAllJsonAsync<T>(string query) where T : KintoneModelBase<T>, new() {
        var cursorRequest = new Dictionary<string, object> {
            ["app"] = this._appID,
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

        return JsonSerializer.Serialize(new { records = allRecords }, this._jsonOptions);
    }

    /// <summary>
    /// レコード件数レスポンス
    /// </summary>
    private sealed class RecordCountResponse {
        /// <summary>
        /// 総件数
        /// </summary>
        [JsonPropertyName("totalCount")]
        public string TotalCountRaw { get; set; } = string.Empty;
        /// <summary>
        /// 総件数（整数型）
        /// </summary>
        [JsonIgnore()]
        public int TotalCount => Convert.ToInt32(this.TotalCountRaw);
    }
}
