using Microsoft.Extensions.Logging;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Application.Interfaces;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

/// <summary>
/// スキーマプロバイダーサービス
/// </summary>
/// <param name="metadataApi"></param>
/// <param name="logger"></param>
public class SchemaProvider(IKintoneAppMetadataApi metadataApi, ILogger<SchemaProvider> logger) : ISchemaProvider {
    private readonly IKintoneAppMetadataApi _metadataApi = metadataApi;
    private readonly ILogger<SchemaProvider> _logger = logger;
    private string? _domain;

    /// <summary>
    /// Kintoneアプリのスキーマ情報を取得します。
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="apiToken"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    public async Task<KintoneAppSchema> GetSchemaAsync(string domain, string apiToken, int appId) {
        this._logger.LogInformation("Fetching metadata for AppId: {appId}", appId);

        var metadata = await this.GetMetadataAsync(domain, apiToken, appId);
        this._logger.LogInformation("Metadata fetched. Converting to schema...");

        var schema = this.ConvertMetadataToSchema(metadata);
        this._logger.LogInformation("Schema conversion completed.");

        return schema;
    }

    /// <summary>
    /// Kintoneアプリのメタデータを取得します。
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="apiToken"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<KintoneAppMetadata> GetMetadataAsync(string domain, string apiToken, int appId) {
        if (this._domain is null) {
            var message = "domainが設定されていません。";
            throw new ArgumentNullException(message);
        }

        return await this._metadataApi.GetAppMetadataAsync(domain, apiToken, appId);
    }

    /// <summary>
    /// Kintoneのサブドメインを設定します。
    /// </summary>
    /// <param name="subDomain"></param>
    public void SetDomain(string subDomain) => this._domain = $"{subDomain}.cybozu.com";

    /// <summary>
    /// Kintoneアプリのメタデータをスキーマに変換します。
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    private KintoneAppSchema ConvertMetadataToSchema(KintoneAppMetadata metadata) {
        return new KintoneAppSchema {
            AppId = metadata.AppId,
            Revision = metadata.Revision,
            Fields = [.. metadata.Fields.Where(f => f.FieldType != KintoneFieldType.SubTable).Select(f => new KintoneFieldSchema {
                FieldCode = f.FieldCode,
                Label = f.FieldLabel,
                FieldType = f.FieldType,
                Required = f.Required,
                Options = f.Options == null ? [] : [.. f.Options], // ドロップダウンなど
                // 必要に応じて追加
            })],
            SubTables = [.. metadata.Fields.Where(st => st.FieldType == KintoneFieldType.SubTable).Select(st => new KintoneSubTableSchema {
                FieldCode = st.FieldCode,
                Label = st.FieldLabel,
                Fields = [.. st.SubFields!.Select(sf => new KintoneFieldSchema {
                    FieldCode = sf.FieldCode,
                    Label = sf.FieldLabel,
                    FieldType = sf.FieldType,
                    Required = sf.Required,
                    Options = sf.Options == null ? [] : [.. sf.Options], // ドロップダウンなど
                    // 必要に応じて追加
                })]
            })]
        };
    }

    /// <summary>
    /// 指定されたバックアップスキーマと現在のスキーマを比較し、差分を取得します。
    /// </summary>
    /// <param name="backupSchema"></param>
    /// <param name="subDomain"></param>
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<KintoneMetadataDiff>> CompareAsync(KintoneAppMetadata backupSchema, string domain, string apiToken, int appId) {
        ArgumentNullException.ThrowIfNull(backupSchema);

        // 最新メタデータ取得
        var latest = await this.GetMetadataAsync(domain, apiToken, appId);
        var diffs = new List<KintoneMetadataDiff>();

        // -----------------------------
        // 追加されたフィールド
        // -----------------------------
        foreach (var f in latest.Fields.Where(f => backupSchema.Fields.All(pf => pf.FieldCode != f.FieldCode))) {
            diffs.Add(new KintoneMetadataDiff { DiffType = KintoneMetadataDiffTypes.Added, After = f });
        }

        // -----------------------------
        // 削除されたフィールド
        // -----------------------------
        foreach (var f in backupSchema.Fields.Where(f => latest.Fields.All(lf => lf.FieldCode != f.FieldCode))) {
            diffs.Add(new KintoneMetadataDiff { DiffType = KintoneMetadataDiffTypes.Removed, Before = f });
        }

        // -----------------------------
        // 変更されたフィールド
        // -----------------------------
        foreach (var latestField in latest.Fields) {
            var prevField = backupSchema.Fields.FirstOrDefault(pf => pf.FieldCode == latestField.FieldCode);
            if (prevField == null) { continue; }

            var changedProps = new List<string>();

            if (prevField.FieldType != latestField.FieldType) { changedProps.Add(nameof(prevField.FieldType)); }
            if (prevField.FieldLabel != latestField.FieldLabel) { changedProps.Add(nameof(prevField.FieldLabel)); }
            if (prevField.Required != latestField.Required) { changedProps.Add(nameof(prevField.Required)); }
            if (!Enumerable.SequenceEqual(prevField.Options ?? [], latestField.Options ?? [])) { changedProps.Add(nameof(prevField.Options)); }
            if (changedProps.Count > 0) {
                diffs.Add(new KintoneMetadataDiff {
                    DiffType = KintoneMetadataDiffTypes.Changed,
                    Before = prevField,
                    After = latestField,
                    ChangedProperties = changedProps,
                    BeforeRevision = backupSchema.Revision,
                    AfterRevision = latest.Revision
                });
            }
        }

        return diffs;
    }
}