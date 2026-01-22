using System.Text.Json;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Helpers;
using KintoneNetLibrary.Infrastructure.Internal;
using Microsoft.Extensions.Logging;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;

namespace KintoneNetLibrary.Infrastructure.Api;

/// <summary>
/// レコード取得（Raw）
/// </summary>
public partial class KintoneApi : BaseKintoneApi, IKintoneApi {
    /// <summary>
    /// IDで単一レコードを取得（Raw）
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<string?> RawFindByIDAsync(string id) {
        if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentNullException(nameof(id)); }

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
    /// IDリストで複数レコードを取得（Raw）
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<string?> RawFindByIDsAsync(IList<string> ids, IList<string>? fieldCodes = null) {
        if (ids == null || ids.Count == 0) { throw new ArgumentNullException(nameof(ids)); }

        var idList = string.Join(",", ids.Select(id => $"\"{id}\""));
        var query = $"id in ({idList})";
        if (ids.Count <= KintoneLimit) {
            var requestUri = KintoneRequestBuilder.BuildFindRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this._appID,
                query,
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
            return await this.RawFindByQueryAsync(query, fieldCodes: fieldCodes);
        }
    }

    /// <summary>
    /// 全レコード取得（条件なし・Raw）
    /// </summary>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    public async Task<string?> RawFindAllAsync(IList<string>? fieldCodes = null) {
        return await this.RawFindBaseJsonAsync(string.Empty, fieldCodes: fieldCodes);
    }

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw）
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<string?> RawFindByFieldAsync(string field, string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        var query = $"{field} = \"{value}\"";
        return await this.RawFindBaseJsonAsync(query);
    }

    /// <summary>
    /// クエリ文字列で検索（Raw）
    /// </summary>
    /// <param name="queryStr"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    public async Task<string?> RawFindByQueryAsync(string queryStr, IList<string>? fieldCodes = null) {
        // LIKE 句のバリデーションは Raw でも同じ
        KintoneQueryValidator.ValidateLikeClause(queryStr, msg => this._logger?.LogWarning(msg));

        return await this.RawFindBaseJsonAsync(queryStr, fieldCodes: fieldCodes);
    }

    /// <summary>
    /// 基本のレコード取得（Raw）
    /// </summary>
    /// <param name="query"></param>
    /// <param name="fieldCodes"></param>
    /// <param name="forceCursor"></param>
    /// <returns></returns>
    /// <exception cref="KintoneException"></exception>
    private async Task<string?> RawFindBaseJsonAsync(
        string query,
        IList<string>? fieldCodes = null,
        bool forceCursor = false) {
        if (!forceCursor) {
            // 件数取得
            var countUri = KintoneRequestBuilder.BuildRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this._appID,
                query,
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

            var countResult = JsonSerializer.Deserialize<RecordCountResponse>(countJson, this._jsonOptions)
                ?? new RecordCountResponse();

            if (countResult.TotalCount > KintoneLimit) {
                return await this.RawCursorFetchAllJsonAsync(query, fieldCodes);
            }
        }

        // 通常取得
        var requestUri = KintoneRequestBuilder.BuildRequestUri(
            this.GetBaseUri(),
            KintoneApiEndpoints.GetRecords,
            this._appID,
            query,
            fieldCodes);

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
    /// カーソルで全件取得（Raw）
    /// </summary>
    /// <param name="query"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    private async Task<string> RawCursorFetchAllJsonAsync(string query, IList<string>? fieldCodes = null) {
        var cursorRequest = new Dictionary<string, object> {
            ["app"] = this._appID,
            ["size"] = this.CursorPageSize,
        };

        if (fieldCodes != null) {
            cursorRequest["fields"] = fieldCodes;
        }

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
}