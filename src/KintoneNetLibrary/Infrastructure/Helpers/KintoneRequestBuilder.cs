using System.Reflection;
using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

/// <summary>
/// Kintone リクエストビルダー
/// </summary>
public static class KintoneRequestBuilder {
    private static readonly JsonSerializerOptions _jsonOptions = DefaultJsonOptions.Default;

    /// <summary>
    /// Kintone レコード作成の JSON を構築
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="records">作成対象のレコードのリスト</param>
    /// <returns>作成用の JSON 文字列</returns>
    /// <exception cref="ArgumentException">レコードが空の場合にスローされます</exception>
    public static string BuildCreateJson<T>(IEnumerable<T> records) where T : KintoneModelBase<T>, new() {
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
    /// Kintone レコード更新の JSON を構築
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="models">更新対象のレコードのリスト</param>
    /// <returns>更新用の JSON 文字列</returns>
    /// <exception cref="ArgumentException">レコードが空の場合にスローされます</exception>
    /// <exception cref="InvalidOperationException">RecordID または IsKey 属性が見つからない場合にスローされます</exception>
    public static string BuildUpdateJson<T>(IList<T> models) where T : KintoneModelBase<T>, new() {
        if (models is null || models.Count == 0) {
            throw new ArgumentException("Models list is null or empty.", nameof(models));
        }

        var records = new List<Dictionary<string, object>>();

        foreach (var model in models) {
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

            var recordFields = model.ToKintoneRecord();
            var keyFieldCodes = model.GetType()
                .GetProperties()
                .Where(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true)
                .Select(p => p.GetCustomAttribute<KintoneItemAttribute>()!.FieldCode)
                .ToHashSet();

            foreach (var key in keyFieldCodes) {
                recordFields.Remove(key);
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
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="models">削除対象のレコードのリスト</param>
    /// <returns>削除用の JSON 文字列</returns>
    /// <exception cref="ArgumentException">レコードが空の場合にスローされます</exception>
    public static string BuildDeleteJson<T>(IEnumerable<T> models) where T : KintoneModelBase<T>, new() {
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

    /// <summary>
    /// Kintone リクエスト URI を構築
    /// </summary>
    /// <param name="baseUri">ベースとなる URI</param>
    /// <param name="path">リクエストパス</param>
    /// <param name="appID">アプリID</param>
    /// <param name="query">検索クエリ</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <param name="additionalParams">追加のクエリパラメータ</param>
    /// <returns>構築された URI</returns>
    public static Uri BuildRequestUri(
        Uri baseUri,
        string path,
        int appID,
        string? query = null,
        IList<string>? fieldCodes = null,
        IDictionary<string, string>? additionalParams = null
        ) {

        var builder = new UriBuilder(new Uri(baseUri, path));
        var parameters = new List<string> {
            $"app={appID}"
        };

        if (!string.IsNullOrWhiteSpace(query)) {
            parameters.Add($"query={Uri.EscapeDataString(query)}");
        }

        if (fieldCodes is { Count: > 0 }) {
            var effectiveFields = EnsureMinimumFields(fieldCodes);
            for (int i = 0; i < effectiveFields.Count; i++) {
                parameters.Add($"fields[{i}]={Uri.EscapeDataString(effectiveFields[i])}");
            }
        }
        if (additionalParams != null) {
            foreach (var kvp in additionalParams) {
                parameters.Add($"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");
            }
        }

        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }

    /// <summary>
    /// Kintone 検索リクエスト URI を構築
    /// </summary>
    /// <param name="baseUri">ベースとなる URI</param>
    /// <param name="path">リクエストパス</param>
    /// <param name="appID">アプリID</param>
    /// <param name="query">検索クエリ</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>構築された URI</returns>
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
    /// <summary>
    /// 最低限必要なフィールドコードを確保
    /// </summary>
    /// <param name="fieldCodes">フィールドコードのリスト</param>
    /// <returns>最低限必要なフィールドコードを含むリスト</returns>
    internal static IList<string> EnsureMinimumFields(IList<string> fieldCodes) {
        var required = new[] { "$id", "$revision" };
        return fieldCodes != null && fieldCodes.Count > 0 ? required.Union(fieldCodes).Distinct().ToArray() : null;
    }
}
