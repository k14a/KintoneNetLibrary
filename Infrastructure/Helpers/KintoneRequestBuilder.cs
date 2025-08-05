using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public static class KintoneRequestBuilder {
    private static readonly JsonSerializerOptions _jsonOptions = DefaultJsonOptions.Default;

    /// <summary>
    /// Kintone レコード登録（複数）の JSON を構築
    /// </summary>
    public static string BuildCreateJson<T>(IEnumerable<T> records) where T : KintoneModelBase {
        var list = records.ToList();
        if (list.Count == 0) {
            throw new ArgumentException("レコードが空です", nameof(records));
        }

        var appID = list.First().AppID;

        var body = new {
            app = appID,
            records = list.Select(r => r.ToKintoneRecord())
        };

        return JsonSerializer.Serialize(body, _jsonOptions);
    }
    /// <summary>
    /// Kintone レコード更新（複数）の JSON を構築
    /// </summary>
    public static string BuildUpdateJson<T>(IList<T> models) where T : KintoneModelBase {
        if (models is null || models.Count == 0) {
            throw new ArgumentException("Models list is null or empty.", nameof(models));
        }

        var records = new List<Dictionary<string, object>>();

        foreach (var model in models) {
            // ToKintoneRecord によってアップロード対象フィールドを取得
            var recordFields = model.ToKintoneRecord();

            var recordWrapper = new Dictionary<string, object>();

            // id または updateKey を指定
            if (!string.IsNullOrWhiteSpace(model.RecordID)) {
                recordWrapper["id"] = model.RecordID;
            } else {
                var keyProp = model.GetType()
                    .GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true) ?? throw new InvalidOperationException("No RecordID or IsKey attribute found for update.");
                var fieldCode = keyProp.GetCustomAttribute<KintoneItemAttribute>()!.FieldCode;
                var value = keyProp.GetValue(model) ?? throw new InvalidOperationException($"Update key property '{fieldCode}' has null value.");
                recordWrapper["updateKey"] = new { field = fieldCode, value = value };
            }

            recordWrapper["record"] = recordFields;
            records.Add(recordWrapper);
        }

        var appId = models.First().AppID;

        var body = new {
            app = appId,
            records
        };

        return JsonSerializer.Serialize(body, _jsonOptions);
    }

    /// <summary>
    /// Kintone レコード削除（複数）の JSON を構築
    /// </summary>
    public static string BuildDeleteJson<T>(IEnumerable<T> models) where T : KintoneModelBase {
        var modelList = models.ToList();
        if (modelList.Count == 0) {
            throw new ArgumentException("Model list is empty", nameof(models));
        }

        var appId = modelList.First().AppID;
        var idList = modelList
            .Select(m => m.RecordID)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        var deleteBody = new {
            app = appId,
            ids = idList
        };

        return JsonSerializer.Serialize(deleteBody, _jsonOptions);
    }
    public static Uri BuildRequestUri(Uri baseUri, string path, int appID, string? query = null, IDictionary<string, string>? additionalParams = null) {

        var builder = new UriBuilder(new Uri(baseUri, path));
        var parameters = new List<string> {
            $"app={appID}"
        };

        if (!string.IsNullOrWhiteSpace(query)) {
            parameters.Add($"query={Uri.EscapeDataString(query)}");
        }

        if (additionalParams != null) {
            foreach (var kvp in additionalParams) {
                parameters.Add($"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");
            }
        }

        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }
    public static Uri BuildFindRequestUri(Uri baseUri, string path, int appID, string? query = null, IList<string>? fieldCodes = null) {
        var effectiveFields = EnsureMinimumFields(fieldCodes);

        var builder = new UriBuilder(new Uri(baseUri, path));
        var parameters = new List<string> { $"app={appID}" };

        if (!string.IsNullOrWhiteSpace(query)) {
            parameters.Add($"query={Uri.EscapeDataString(query)}");
        }

        if (fieldCodes is { Count: > 0 }) {
            for (int i = 0; i < fieldCodes.Count; i++) {
                parameters.Add($"fields[{i}]={Uri.EscapeDataString(fieldCodes[i])}");
            }
        }

        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }
    internal static IList<string> EnsureMinimumFields(IList<string> fieldCodes) {
        var required = new[] { "$id", "$revision" };
        return fieldCodes != null && fieldCodes.Count > 0 ? required.Union(fieldCodes).Distinct().ToArray() : null;
    }
}
