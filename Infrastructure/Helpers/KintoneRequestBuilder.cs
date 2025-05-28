using System.Reflection;
using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public static class KintoneRequestBuilder {
    private static readonly JsonSerializerOptions _jsonOptions = KintoneJsonOptions.Default;

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
            var recordFields = new Dictionary<string, object>();

            // アップロード対象のフィールドのみ追加
            foreach (var prop in model.GetType().GetProperties()) {
                var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
                if (attr is null || !attr.IsUpload) {
                    continue;
                }

                var fieldCode = attr.FieldCode;
                var value = prop.GetValue(model);
                recordFields[fieldCode] = new { value };
            }

            // 各レコードオブジェクト
            var recordWrapper = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(model.RecordID)) {
                recordWrapper["id"] = model.RecordID;
            } else {
                var keyProp = model.GetType()
                    .GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

                if (keyProp == null) {
                    throw new InvalidOperationException("No ID or update key specified in the model.");
                }

                var fieldCode = keyProp.GetCustomAttribute<KintoneItemAttribute>()!.FieldCode;
                var value = keyProp.GetValue(model);

                recordWrapper["updateKey"] = new {
                    field = fieldCode,
                    value = value
                };
            }

            recordWrapper["record"] = recordFields;

            records.Add(recordWrapper);
        }

        var appId = models.First().AppID;

        var body = new {
            app = appId,
            records = records
        };

        return JsonSerializer.Serialize(body);
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
    public static Uri BuildRequestUri(Uri baseUri, string path, int appID, string? rawQuery = null) {
        var builder = new UriBuilder(new Uri(baseUri, path));
        var parameters = new List<string> { $"app={appID}" };

        if (!string.IsNullOrEmpty(rawQuery)) {
            // クエリが既に query=... 形式で渡されていたらそのまま使う
            if (!rawQuery.TrimStart().StartsWith("query=")) {
                rawQuery = $"query={Uri.EscapeDataString(rawQuery)}";
            }
            parameters.Add(rawQuery);
        }

        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }

}
