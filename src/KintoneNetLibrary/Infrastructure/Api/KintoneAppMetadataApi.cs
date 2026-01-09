using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Infrastructure.Internal;
using KintoneNetLibrary.Domain.Entities;
using System.Text.Json;
using KintoneNetLibrary.Domain.Converters;

namespace KintoneNetLibrary.Infrastructure.Api;

// コメントは日本語で記述
/// <summary>
/// Kintoneアプリのメタデータを取得するAPIクラス
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
/// <param name="access"></param>
/// <param name="httpClient"></param>
/// <param name="logger"></param>
public class KintoneAppMetadataApi(
    KintoneAccessBase access,
    HttpClient httpClient,
    ILogger<KintoneAppMetadataApi>? logger = null) : BaseKintoneApi(access, httpClient, logger), IKintoneAppMetadataApi {

    /// <summary>
    /// スキップするフィールドタイプのセット
    /// </summary>
    private static readonly HashSet<string> _skippedFieldTypes = new(StringComparer.OrdinalIgnoreCase) { "GROUP", "SPACER", "HR" };

    /// <summary>
    /// 指定したアプリのフィールド情報をJSON形式で取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    public async Task<string> GetFieldsJsonAsync(int appId) {
        var uri = this.BuildRequestUri(KintoneApiEndpoints.GetAppFields, $"app={appId}");
        return await this.SendGetAsync(uri);
    }

    /// <summary>
    /// 指定したアプリのレイアウト情報をJSON形式で取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    public async Task<string> GetLayoutJsonAsync(int appId) {
        var uri = this.BuildRequestUri(KintoneApiEndpoints.GetAppLayout, $"app={appId}");
        return await this.SendGetAsync(uri);
    }

    /// <summary>
    /// 指定したアプリのメタデータを取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    public async Task<KintoneAppMetadata> GetAppMetadataAsync(int appId, string apiToken) {
        var json = await this.GetFieldsJsonAsync(appId);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var properties = root.GetProperty("properties");

        var fields = new List<KintoneFieldMetadata>();

        foreach (var prop in properties.EnumerateObject()) {
            var code = prop.Name;
            var field = prop.Value;

            var type = field.GetProperty("type").GetString()!;
            var label = field.GetProperty("label").GetString() ?? code;

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
                        Code = sfCode,
                        Label = sfValue.GetProperty("label").GetString() ?? sfCode,
                        Type = fieldType,
                        Required = sfValue.TryGetProperty("required", out var req) && req.GetBoolean(),
                        Options = ExtractOptions(sfValue)
                    });
                }

                fields.Add(new KintoneFieldMetadata {
                    Code = code,
                    Label = label,
                    Type = KintoneFieldType.SubTable,
                    Required = false,
                    SubFields = subFields
                });

            } else {
                // 通常フィールド
                if (!KintoneFieldTypeMapper.TryConvert(type, out var fieldType)) { continue; }

                fields.Add(new KintoneFieldMetadata {
                    Code = code,
                    Label = label,
                    Type = fieldType,
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
    /// <param name="field"></param>
    /// <returns></returns>
    private static IReadOnlyList<string>? ExtractOptions(JsonElement field) {
        if (!field.TryGetProperty("options", out var optionsJson)) { return null; }

        return [.. optionsJson.EnumerateObject().Select(o => o.Value.GetProperty("label").GetString()!)];
    }
}
