using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Infrastructure.Internal;
using KintoneNetLibrary.Domain.Entities;
using System.Text.Json;
using KintoneNetLibrary.Domain.Converters;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Infrastructure.Api;

/// <summary>
/// kintoneのアプリメタデータAPIクライアント実装
/// </summary>
/// <param name="httpClientFactory">HTTPクライアントファクトリ</param>
/// <param name="logger">ロガー</param>
public class KintoneAppMetadataApi(IHttpClientFactory httpClientFactory, ILogger<KintoneAppMetadataApi> logger) : IKintoneAppMetadataApi {
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<KintoneAppMetadataApi> _logger = logger;

    // ---------------------------------------------------------
    // 共通ユーティリティ（BaseKintoneApi の代替）
    // ---------------------------------------------------------
    /// <summary>
    /// APIリクエスト用のURIを構築します。
    /// </summary>
    /// <param name="domain">kintoneのドメイン</param>
    /// <param name="path">APIのパス</param>
    /// <param name="query">クエリ文字列（オプション）</param>
    /// <returns>構築されたURI</returns>
    private static Uri BuildRequestUri(string domain, string path, string? query = null) {
        var baseUri = new Uri($"https://{domain.TrimEnd('/')}/k/v1/");
        var builder = new UriBuilder(new Uri(baseUri, path));

        if (!string.IsNullOrEmpty(query)) { builder.Query = query; }

        return builder.Uri;
    }

    /// <summary>
    /// APIトークンをHTTPリクエストに適用します。
    /// </summary>
    /// <param name="request">HTTPリクエストメッセージ</param>
    /// <param name="apiToken">APIトークン</param>
    private static void ApplyAuth(HttpRequestMessage request, string apiToken) {
        request.Headers.Add("X-Cybozu-API-Token", apiToken);
    }

    /// <summary>
    /// GETリクエストを送信し、レスポンスのJSONを文字列として返します。エラーが発生した場合はKintoneExceptionをスローします。
    /// </summary>
    /// <param name="uri">リクエストURI</param>
    /// <param name="apiToken">APIトークン</param>
    /// <returns>レスポンスのJSON文字列</returns>
    /// <exception cref="KintoneException">APIリクエストが失敗した場合にスローされます</exception>
    private async Task<string> SendGetAsync(Uri uri, string apiToken) {
        var client = this._httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        ApplyAuth(request, apiToken);

        using var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        this._logger?.LogTrace(json);

        if (!response.IsSuccessStatusCode) {
            var message = $"APIリクエストに失敗しました。StatusCode: {response.StatusCode}, Response: {json}";
            this._logger.LogError(message);
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return json;
    }
    // ---------------------------------------------------------
    // API実装
    // ---------------------------------------------------------
    /// <summary>
    /// 指定したアプリのフィールド情報をJSON形式で取得します。
    /// </summary>
    /// <param name="domain">kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリID</param>
    /// <returns>フィールド情報のJSON文字列</returns>
    public async Task<string> GetFieldsJsonAsync(string domain, string apiToken, int appId) {
        var uri = BuildRequestUri(domain, KintoneApiEndpoints.GetAppFields, $"app={appId}");
        return await this.SendGetAsync(uri, apiToken);
    }

    /// <summary>
    /// 指定したアプリのレイアウト情報をJSON形式で取得します。
    /// </summary>
    /// <param name="domain">kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリID</param>
    /// <returns>レイアウト情報のJSON文字列</returns>
    public async Task<string> GetLayoutJsonAsync(string domain, string apiToken, int appId) {
        var uri = BuildRequestUri(domain, KintoneApiEndpoints.GetAppLayout, $"app={appId}");
        return await this.SendGetAsync(uri, apiToken);
    }

    /// <summary>
    /// 指定したアプリのメタデータを取得します。
    /// </summary>
    /// <param name="domain">kintoneのドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリID</param>
    /// <returns>アプリのメタデータ</returns>
    public async Task<KintoneAppMetadata> GetAppMetadataAsync(string domain, string apiToken, int appId) {
        var json = await this.GetFieldsJsonAsync(domain, apiToken, appId);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var properties = root.GetProperty("properties");

        var fields = new List<KintoneFieldMetadata>();

        foreach (var prop in properties.EnumerateObject()) {
            var code = prop.Name;
            var field = prop.Value;

            var type = field.GetProperty("type").GetString()!;
            var label = field.GetProperty("label").GetString() ?? code;
            var OriginalFieldType = field.GetProperty("type").GetString()!;

            if (type == "SUBTABLE") {
                // サブテーブル
                var subFieldsJson = field.GetProperty("fields");

                var subFields = new List<KintoneFieldMetadata>();

                foreach (var sf in subFieldsJson.EnumerateObject()) {
                    var sfCode = sf.Name;
                    var sfValue = sf.Value;
                    var sfType = sfValue.GetProperty("type").GetString()!;
                    if (!KintoneFieldTypeMapper.TryConvert(sfType, out var fieldType)) { continue; }

                    subFields.Add(new KintoneFieldMetadata {
                        FieldCode = sfCode,
                        FieldLabel = sfValue.GetProperty("label").GetString() ?? sfCode,
                        FieldType = fieldType,
                        FieldTypeName = fieldType.ToString(),
                        OriginalFieldType = sfType,
                        Required = sfValue.TryGetProperty("required", out var req) && req.GetBoolean(),
                        Options = ExtractOptions(sfValue)
                    });
                }

                fields.Add(new KintoneFieldMetadata {
                    FieldCode = code,
                    FieldLabel = label,
                    FieldType = KintoneFieldType.SubTable,
                    FieldTypeName = KintoneFieldType.SubTable.ToString(),
                    OriginalFieldType = type,
                    Required = false,
                    SubFields = subFields
                });

            } else {
                // 通常フィールド
                if (!KintoneFieldTypeMapper.TryConvert(type, out var fieldType)) { continue; }

                fields.Add(new KintoneFieldMetadata {
                    FieldCode = code,
                    FieldLabel = label,
                    FieldType = fieldType,
                    FieldTypeName = fieldType.ToString(),
                    OriginalFieldType = type,
                    Required = field.TryGetProperty("required", out var req) && req.GetBoolean(),
                    Options = ExtractOptions(field)
                });
            }
        }

        var revisionString = root.GetProperty("revision").GetString();
        if (!int.TryParse(revisionString, out var revision)) { revision = 0; }

        return new KintoneAppMetadata {
            AppId = appId,
            Revision = revision,
            Fields = fields
        };
    }

    /// <summary>
    /// 選択肢を抽出します。
    /// </summary>
    /// <param name="field">フィールドのJSON要素</param>
    /// <returns>選択肢のリスト</returns>
    private static IReadOnlyList<string>? ExtractOptions(JsonElement field) {
        if (!field.TryGetProperty("options", out var optionsJson)) { return null; }

        return [.. optionsJson
            .EnumerateObject()
            .OrderBy(o => o.Value.GetProperty("label").GetString()!)
            .Select(o => o.Value.GetProperty("label").GetString()!)];
    }
    /// <summary>
    /// スキップするフィールドタイプのセット
    /// </summary>
    private static readonly HashSet<string> _skippedFieldTypes = new(StringComparer.OrdinalIgnoreCase) { "GROUP", "SPACER", "HR" };
}
