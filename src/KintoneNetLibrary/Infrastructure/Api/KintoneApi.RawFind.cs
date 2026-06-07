using System.Text;
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
public partial class KintoneApi : IKintoneApi {

    /// <summary>
    /// Idで単一レコードを取得（Raw）
    /// </summary>
    /// <param name="id">取得するレコードのId</param>
    /// <returns>取得したレコードのJSON文字列</returns>
    /// <exception cref="ArgumentNullException">id が null または空白の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone API からのエラーが発生した場合にスローされます</exception>
    public async Task<string?> RawFindByIdAsync(string id) {
        if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentNullException(nameof(id)); }

        var requestUri = this.BuildRequestUri(KintoneApiEndpoints.GetSingleRecord, $"app={this._appId}&id={id}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (this._logger != null) {
            _logTraceException(this._logger, json, null);
        }

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return json;
    }

    /// <summary>
    /// Idで単一レコードを取得（Raw）
    /// </summary>
    /// <param name="output">取得したレコードのJSON文字列を書き込むストリーム</param>
    /// <param name="id">取得するレコードのId</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">id が null または空白の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone API からのエラーが発生した場合にスローされます</exception>
    public async Task RawFindByIdAsStreamAsync(
        Stream output,
        string id) {
        if (string.IsNullOrWhiteSpace(id)) {
            throw new ArgumentNullException(nameof(id));
        }

        var requestUri = this.BuildRequestUri(KintoneApiEndpoints.GetSingleRecord, $"app={this._appId}&id={id}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (this._logger != null) {
            _logTraceException(this._logger, json, null);
        }

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        await output.WriteAsync(bytes);
    }

    /// <summary>
    /// Idリストで複数レコードを取得（Raw）
    /// </summary>
    /// <param name="ids">取得するレコードのIdリスト</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>取得したレコードのJSON文字列</returns>
    /// <exception cref="ArgumentNullException">ids が null または空の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone API からのエラーが発生した場合にスローされます</exception>
    public async Task<string?> RawFindByIdsAsync(IList<string> ids, IList<string>? fieldCodes = null) {
        if (ids == null || ids.Count == 0) { throw new ArgumentNullException(nameof(ids)); }

        var idList = string.Join(",", ids.Select(id => $"\"{id}\""));
        var query = $"id in ({idList})";
        if (ids.Count <= KintoneLimit) {
            var requestUri = KintoneRequestBuilder.BuildFindRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this._appId,
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
    /// Idリストで複数レコードを取得（Raw）
    /// </summary>
    /// <param name="output">取得したレコードのJSON文字列を書き込むストリーム</param>
    /// <param name="ids">取得するレコードのIdリスト</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">ids が null または空の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone API からのエラーが発生した場合にスローされます</exception>
    public async Task RawFindByIdsAsStreamAsync(
        Stream output,
        IList<string> ids,
        IList<string>? fieldCodes = null) {
        if (ids == null || ids.Count == 0) {
            throw new ArgumentNullException(nameof(ids));
        }

        var idList = string.Join(",", ids.Select(id => $"\"{id}\""));
        var query = $"id in ({idList})";

        if (ids.Count <= KintoneLimit) {
            // 通常 API で取得してそのまま Stream に書き込む
            var requestUri = KintoneRequestBuilder.BuildFindRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this._appId,
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

            var bytes = Encoding.UTF8.GetBytes(json);
            await output.WriteAsync(bytes);
        } else {
            // 大規模データ → Stream ベースの RawFindBaseJsonAsStreamAsync を使う
            await this.RawFindBaseJsonAsStreamAsync(output, query, fieldCodes);
        }
    }

    /// <summary>
    /// 全レコード取得（条件なし・Raw）
    /// </summary>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>取得したレコードのJSON文字列</returns>
    public async Task<string?> RawFindAllAsync(IList<string>? fieldCodes = null) {
        return await this.RawFindBaseJsonAsync(string.Empty, fieldCodes: fieldCodes);
    }

    /// <summary>
    /// 全レコード取得（条件なし・Raw）
    /// </summary>
    /// <param name="output">取得したレコードのJSON文字列を書き込むストリーム</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    public async Task RawFindAllAsStreamAsync(Stream output, IList<string>? fieldCodes = null) {
        await this.RawFindBaseJsonAsStreamAsync(output, string.Empty, fieldCodes);
    }

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw）
    /// </summary>
    /// <param name="field">検索するフィールドコード</param>
    /// <param name="value">検索する値</param>
    /// <returns>取得したレコードのJSON文字列</returns>
    public async Task<string?> RawFindByFieldAsync(string field, string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        var query = $"{field} = \"{value}\"";
        return await this.RawFindBaseJsonAsync(query);
    }

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw）
    /// </summary>
    /// <param name="output">取得したレコードのJSON文字列を書き込むストリーム</param>
    /// <param name="field">検索するフィールドコード</param>
    /// <param name="value">検索する値</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns></returns>
    public async Task RawFindByFieldAsStreamAsync(Stream output, string field, string value, IList<string>? fieldCodes = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        var query = $"{field} = \"{value}\"";
        await this.RawFindBaseJsonAsStreamAsync(output, query, fieldCodes);
    }

    /// <summary>
    /// クエリ文字列で検索（Raw）
    /// </summary>
    /// <param name="queryStr">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>取得したレコードのJSON文字列</returns>
    public async Task<string?> RawFindByQueryAsync(string queryStr, IList<string>? fieldCodes = null) {
        // LIKE 句のバリデーションは Raw でも同じ
        KintoneQueryValidator.ValidateLikeClause(queryStr, msg => { if (this._logger != null) { _logWarningException(this._logger, msg, null); } });

        try {
            return await this.RawFindBaseJsonAsync(queryStr, fieldCodes: fieldCodes);
        } catch (OutOfMemoryException oom) {
            throw new KintoneException(
                "大量データを string として取得したため、メモリ不足 (OutOfMemoryException) が発生しました。" +
                "大規模データを扱う場合は、RawFindByQueryAsStreamAsync を使用することで" +
                "メモリ使用量を抑えて安全に処理できます。",
                oom);
        }
    }

    /// <summary>
    /// クエリ文字列で検索（Raw）
    /// </summary>
    /// <param name="output">取得したレコードのJSON文字列を書き込むストリーム</param>
    /// <param name="queryStr">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns></returns>
    public async Task RawFindByQueryAsStreamAsync(Stream output, string queryStr, IList<string>? fieldCodes = null) {
        KintoneQueryValidator.ValidateLikeClause(queryStr, msg => { if (this._logger != null) { _logWarningException(this._logger, msg, null); } });

        await this.RawFindBaseJsonAsStreamAsync(output, queryStr, fieldCodes: fieldCodes);
    }

    /// <summary>
    /// 基本のレコード取得（Raw）
    /// </summary>
    /// <param name="query">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <param name="forceCursor">カーソルを強制的に使用するかどうか</param>
    /// <returns>取得したレコードのJSON文字列</returns>
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
                this._appId,
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
                using var ms = new MemoryStream();
                await this.RawCursorFetchAllJsonAsStreamAsync(ms, query, fieldCodes);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        // 通常取得
        var requestUri = KintoneRequestBuilder.BuildRequestUri(
            this.GetBaseUri(),
            KintoneApiEndpoints.GetRecords,
            this._appId,
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
    /// 基本のレコード取得（Raw）
    /// </summary>
    /// <param name="output">取得したレコードのJSON文字列を書き込むストリーム</param>
    /// <param name="query">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <param name="forceCursor">カーソルを強制的に使用するかどうか</param>
    /// <exception cref="KintoneException">Kintone API の呼び出し中にエラーが発生した場合にスローされます</exception>
    private async Task RawFindBaseJsonAsStreamAsync(Stream output, string query, IList<string>? fieldCodes = null, bool forceCursor = false) {
        if (!forceCursor) {
            // 件数取得
            var countUri = KintoneRequestBuilder.BuildRequestUri(
                this.GetBaseUri(),
                KintoneApiEndpoints.GetRecords,
                this._appId,
                query,
                fieldCodes,
                new Dictionary<string, string> {
                    { "totalCount", "true" },
                    { "limit", "1" },
                }
            );

            using var countReq = new HttpRequestMessage(HttpMethod.Get, countUri);
            this.SetHeaders(countReq);

            using var countResp = await this._httpClient.SendAsync(countReq);
            var countJson = await countResp.Content.ReadAsStringAsync();

            if (!countResp.IsSuccessStatusCode) {
                throw new KintoneException(KintoneErrorConverter.Parse(countJson));
            }

            var countResult = JsonSerializer.Deserialize<RecordCountResponse>(countJson, this._jsonOptions)
                ?? new RecordCountResponse();

            // 大規模データ → カーソルでストリーミング
            if (countResult.TotalCount > KintoneLimit) {
                await this.RawCursorFetchAllJsonAsStreamAsync(output, query, fieldCodes);
                return;
            }
        }

        // 小規模データ → 通常 API の JSON をそのまま書き込む
        var requestUri = KintoneRequestBuilder.BuildRequestUri(this.GetBaseUri(), KintoneApiEndpoints.GetRecords, this._appId, query, fieldCodes);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        this.SetHeaders(request);

        using var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        // 小規模データはそのまま Stream に書く
        var bytes = Encoding.UTF8.GetBytes(json);
        await output.WriteAsync(bytes);
    }

    /// <summary>
    /// カーソルで全レコード取得（Raw）
    /// </summary>
    /// <param name="output">取得したレコードのJSON文字列を書き込むストリーム</param>
    /// <param name="query">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>取得したレコードのJSON文字列を書き込むストリーム</returns>
    private async Task RawCursorFetchAllJsonAsStreamAsync(Stream output, string query, IList<string>? fieldCodes = null) {
        var cursorRequest = new Dictionary<string, object> {
            ["app"] = this._appId,
            ["size"] = this.CursorPageSize,
        };

        if (fieldCodes != null) {
            cursorRequest["fields"] = fieldCodes;
        }

        if (!string.IsNullOrEmpty(query)) {
            cursorRequest["query"] = query;
        }

        var cursor = await this.CreateCursorAsync(cursorRequest);

        using var writer = new Utf8JsonWriter(output);

        writer.WriteStartObject();
        writer.WritePropertyName("records");
        writer.WriteStartArray();

        while (true) {
            using var stream = await this.FetchCursorStreamAsync(cursor);

            var hasNext = this.ReadRecordsAndNextFromCursorStream(stream, record => {
                record.WriteTo(writer);
            });

            if (!hasNext) { break; }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
    }

    /// <summary>
    /// カーソルストリームからレコードと next を読み取る
    /// </summary>
    /// <param name="stream">カーソルストリーム</param>
    /// <param name="onRecord">レコードを処理するデリゲート</param>
    /// <returns>次のレコードが存在するかどうか</returns>
    private bool ReadRecordsAndNextFromCursorStream(Stream stream, Action<JsonElement> onRecord) {
        bool hasNext = false;

        byte[] buffer = new byte[64 * 1024];
        byte[] leftover = Array.Empty<byte>();

        int bytesRead;
        bool insideRecords = false;

        JsonReaderState state = default;

        try {
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
                // leftover + buffer を結合
                var data = new byte[leftover.Length + bytesRead];
                Buffer.BlockCopy(leftover, 0, data, 0, leftover.Length);
                Buffer.BlockCopy(buffer, 0, data, leftover.Length, bytesRead);

                // ★ state を維持する
                var reader = new Utf8JsonReader(data, isFinalBlock: false, state);

                while (reader.Read()) {
                    if (reader.TokenType == JsonTokenType.PropertyName) {
                        if (reader.ValueTextEquals("next")) {
                            reader.Read();
                            hasNext = reader.GetBoolean();
                        } else if (reader.ValueTextEquals("records")) {
                            reader.Read(); // StartArray
                            insideRecords = true;
                        }
                        continue;
                    }

                    if (insideRecords && reader.TokenType == JsonTokenType.StartObject) {
                        // ★ チャンクをまたいでも壊れない JSON オブジェクト読み取り
                        using var doc = ReadOneJsonObject(ref reader, stream, ref state, ref leftover);
                        onRecord(doc.RootElement.Clone());
                    }
                }

                // ★ 次チャンクに渡す leftover を更新
                leftover = data.AsSpan((int)reader.BytesConsumed).ToArray();

                // ★ state を保存
                state = reader.CurrentState;
            }

            return hasNext;
        } catch (Exception ex) {
            if(this._logger != null) {
                _logErrorException(this._logger, "Error while reading records from cursor stream.", ex);
            }
            throw;
        }
    }

    /// <summary>
    /// Utf8JsonReader を使って、チャンクをまたいで 1 つの JSON オブジェクトを読み取る
    /// </summary>
    /// <param name="reader">現在の Utf8JsonReader</param>
    /// <param name="stream">カーソルストリーム</param>
    /// <param name="state">現在の JsonReaderState</param>
    /// <param name="leftover">前のチャンクからの残りのデータ</param>
    /// <returns></returns>
    /// <exception cref="JsonException"></exception>
    private static JsonDocument ReadOneJsonObject(
        ref Utf8JsonReader reader,
        Stream stream,
        ref JsonReaderState state,
        ref byte[] leftover) {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

        int depth = 0;

        // StartObject はすでに reader.TokenType == StartObject
        writer.WriteStartObject();
        depth++;

        while (true) {
            // 現在のチャンクを読み進める
            while (reader.Read()) {
                switch (reader.TokenType) {
                    case JsonTokenType.StartObject:
                        writer.WriteStartObject();
                        depth++;
                        break;

                    case JsonTokenType.EndObject:
                        writer.WriteEndObject();
                        depth--;
                        if (depth == 0) {
                            writer.Flush();
                            ms.Position = 0;
                            return JsonDocument.Parse(ms);
                        }
                        break;

                    case JsonTokenType.StartArray:
                        writer.WriteStartArray();
                        break;

                    case JsonTokenType.EndArray:
                        writer.WriteEndArray();
                        break;

                    case JsonTokenType.PropertyName:
                        writer.WritePropertyName(reader.GetString()!);
                        break;

                    case JsonTokenType.String:
                        writer.WriteStringValue(reader.GetString());
                        break;

                    case JsonTokenType.Number:
                        writer.WriteNumberValue(reader.GetDouble());
                        break;

                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        writer.WriteBooleanValue(reader.GetBoolean());
                        break;

                    case JsonTokenType.Null:
                        writer.WriteNullValue();
                        break;
                }
            }

            // 現在のチャンクを読み切ったので次のチャンクを読む
            byte[] buffer = new byte[64 * 1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead == 0) {
                throw new JsonException("Incomplete JSON object in stream.");
            }

            // leftover と結合して次の Utf8JsonReader に渡す
            var data = new byte[leftover.Length + bytesRead];
            Buffer.BlockCopy(leftover, 0, data, 0, leftover.Length);
            Buffer.BlockCopy(buffer, 0, data, leftover.Length, bytesRead);

            // 新しい reader を作成（state を維持）
            reader = new Utf8JsonReader(data, isFinalBlock: false, state);

            // 次チャンクに備えて leftover を更新
            leftover = data.AsSpan((int)reader.BytesConsumed).ToArray();

            // state を更新
            state = reader.CurrentState;
        }
    }

    /// <summary>
    /// カーソルをストリームで取得します
    /// </summary>
    /// <param name="cursorId">カーソルId</param>
    /// <returns>取得したレコードのJSON文字列を書き込むストリーム</returns>
    public async IAsyncEnumerable<Stream> StreamCursorStreamAsync(string cursorId) {
        try {
            while (true) {
                var stream = await this.FetchCursorStreamAsync(cursorId);

                using var doc = await JsonDocument.ParseAsync(stream);
                bool hasNext = doc.RootElement.TryGetProperty("next", out var nextProp) && nextProp.GetBoolean();

                stream.Position = 0;
                yield return stream;

                if (!hasNext) { break; }
            }

        } finally {
            var deleteJson = JsonSerializer.Serialize(new { id = cursorId }, this._jsonOptions);
            await this.DeleteCursorJsonAsync(deleteJson);
        }
    }

    /// <summary>
    /// カーソルページをストリームで取得します
    /// </summary>
    /// <param name="query">クエリ文字列</param>
    /// <param name="fields">取得するフィールドのリスト</param>
    /// <param name="size">1ページあたりの取得件数</param>
    /// <returns>取得したレコードのJSON文字列を書き込むストリーム</returns>
    public async IAsyncEnumerable<Stream> StreamCursorPagesAsync(string query, IList<string>? fields = null, int? size = null) {
        // カーソル作成
        var cursorId = await this.CreateCursorAsync(query, fields, size);

        try {
            while (true) {
                // 1ページ分の JSON をストリームで取得
                var rawStream = await this.FetchCursorPageAsStreamAsync(cursorId);

                var ms = new MemoryStream();
                await rawStream.CopyToAsync(ms);
                ms.Position = 0;

                // next（または done）を判定するために一度だけパース
                using var doc = await JsonDocument.ParseAsync(ms);
                bool hasNext =
                    (doc.RootElement.TryGetProperty("next", out var nextProp) && nextProp.GetBoolean()) ||
                    (doc.RootElement.TryGetProperty("done", out var doneProp) && !doneProp.GetBoolean());

                // 呼び出し側に返すために stream を巻き戻す
                ms.Position = 0;

                yield return ms;

                if (!hasNext) { break; }
            }

        } finally {
            // カーソル削除
            try {
                await this.DeleteCursorAsync(cursorId);
            } catch (KintoneException ex) when (ex.Detail.Contains("GAIA_CN01")) {
                // すでに削除済みなど
                if(this._logger != null) {
                    _logWarningException(this._logger, ex.Message,ex);
                }
            }
        }
    }

    /// <summary>
    /// レコードをストリームで取得します
    /// </summary>
    /// <param name="query">クエリ文字列</param>
    /// <param name="fields">取得するフィールドのリスト</param>
    /// <param name="size">1ページあたりの取得件数</param>
    /// <returns>取得したレコードの JSON 要素の列挙</returns>
    public async IAsyncEnumerable<JsonElement> StreamRecordsAsync(string query, IList<string>? fields = null, int? size = null) {
        await foreach (var pageStream in this.StreamCursorPagesAsync(query, fields, size)) {
            // ページ JSON を Utf8JsonReader でパース
            using var doc = await JsonDocument.ParseAsync(pageStream);

            if (!doc.RootElement.TryGetProperty("records", out var recordsElement)) { continue; }

            foreach (var record in recordsElement.EnumerateArray()) {
                // JsonElement は使い捨てなので Clone() して返す
                yield return record.Clone();
            }
        }
    }

}
