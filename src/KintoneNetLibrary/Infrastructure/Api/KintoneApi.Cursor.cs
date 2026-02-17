using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Infrastructure.Internal;
using KintoneNetLibrary.Application.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Api;

/// <summary>
/// Kintone API のカーソル操作に関する機能を提供します。
/// </summary>
public partial class KintoneApi : IKintoneApi {
    /// <summary>
    /// カーソルを作成します。
    /// </summary>
    /// <param name="body">カーソル作成に必要なパラメータを含む辞書</param>
    /// <returns>作成されたカーソルのID</returns>
    /// <exception cref="KintoneException">カーソル作成に失敗した場合にスローされます</exception>
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

    /// <summary>
    /// カーソルを作成します。
    /// </summary>
    /// <param name="query">クエリ文字列</param>
    /// <param name="fields">取得するフィールドのリスト</param>
    /// <param name="size">取得する件数</param>
    /// <returns>作成されたカーソルのID</returns>
    public Task<string> CreateCursorAsync(string query, IList<string>? fields = null, int? size = null) {
        var body = new Dictionary<string, object> {
            { "app", this._appID },
            { "query", query }
        };

        if (fields is not null) {
            body["fields"] = fields;
        }

        if (size is not null) {
            body["size"] = size.Value;
        }

        return this.CreateCursorAsync(body);
    }

    /// <summary>
    /// カーソルを取得します。
    /// </summary>
    /// <param name="cursorId">取得するカーソルのID</param>
    /// <returns>取得したカーソルのJSON文字列</returns>
    /// <exception cref="KintoneException">カーソル取得に失敗した場合にスローされます</exception>
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

    /// <summary>
    /// カーソルをストリームで取得します。
    /// </summary>
    /// <param name="cursorId">取得するカーソルのID</param>
    /// <returns>取得したカーソルのストリーム</returns>
    public Task<Stream> FetchCursorPageAsStreamAsync(string cursorId) {
        return this.FetchCursorStreamAsync(cursorId);
    }

    /// <summary>
    /// カーソルをストリームで取得します。
    /// </summary>
    /// <param name="cursorId">取得するカーソルのID</param>
    /// <returns>取得したカーソルのストリーム</returns>
    /// <exception cref="KintoneException">カーソル取得に失敗した場合にスローされます</exception>
    private async Task<Stream> FetchCursorStreamAsync(string cursorId) {
        var endpoint = $"records/cursor.json?id={cursorId}";
        var requestUri = $"{this.GetBaseUri()}{endpoint}";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        var response = await this._httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead
        );

        if (!response.IsSuccessStatusCode) {
            var json = await response.Content.ReadAsStringAsync();
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return await response.Content.ReadAsStreamAsync();
    }

    /// <summary>
    /// カーソルを削除します。
    /// </summary>
    /// <param name="json">削除するカーソルのJSON文字列</param>
    /// <returns>削除されたカーソルのJSON文字列</returns>
    /// <exception cref="KintoneException">カーソル削除に失敗した場合にスローされます</exception>
    public async Task<string> DeleteCursorJsonAsync(string json) {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{this.GetBaseUri()}{KintoneApiEndpoints.Cursor}") {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        this.SetHeaders(request);

        var response = await this._httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            this._logger?.LogError("DeleteCursorJsonAsync failed: {Json}", responseJson);
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /// <summary>
    /// カーソルを削除します。
    /// </summary>
    /// <param name="cursorId">削除するカーソルのID</param>
    /// <returns>削除されたカーソルのJSON文字列</returns>
    public Task DeleteCursorAsync(string cursorId) {
        var json = JsonSerializer.Serialize(new { id = cursorId }, this._jsonOptions);
        return this.DeleteCursorJsonAsync(json);
    }

    /// <summary>
    /// カーソルをストリームとして取得します。
    /// </summary>
    /// <param name="cursorId">取得するカーソルのID</param>
    /// <returns>取得したカーソルのストリーム</returns>
    public async IAsyncEnumerable<Stream> StreamCursorAsync(string cursorId) {
        try {
            while (true) {
                var stream = await this.FetchCursorStreamAsync(cursorId);

                using var doc = await JsonDocument.ParseAsync(stream);
                var hasNext = doc.RootElement.TryGetProperty("next", out var doneProp) && doneProp.GetBoolean();

                stream.Position = 0; // 再利用のため巻き戻す
                yield return stream;

                if (!hasNext) { break; }
            }

        } finally {
            var deleteRequestJson = JsonSerializer.Serialize(new { id = cursorId }, this._jsonOptions);
            try {
                await this.DeleteCursorJsonAsync(deleteRequestJson);
            } catch (KintoneException ex) when (ex.Detail.Contains("GAIA_CN01")) {
                this._logger?.LogWarning("DeleteCursorJsonAsync failed with GAIA_CN01: {Exception}", ex.ToString());
            }
        }
    }

    /// <summary>
    /// カーソル作成のレスポンス DTO
    /// </summary>
    private sealed class CursorCreated {
        /// <summary>
        /// 作成したカーソル ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    /// <summary>
    /// カーソル取得のレスポンス DTO
    /// </summary>
    /// <typeparam name="TRecord">Kintoneのレコードデータの型</typeparam>
    public sealed class CursorFetch<TRecord> {
        /// <summary>
        /// 取得したレコード一覧
        /// </summary>
        [JsonPropertyName("records")]
        public IList<TRecord> Records { get; set; } = [];
        /// <summary>
        /// カーソルの完了状態
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
