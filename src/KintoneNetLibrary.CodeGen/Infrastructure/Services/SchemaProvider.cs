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
public class SchemaProvider(IKintoneAppMetadataApi metadataApi, IMetadataConverter converter, ILogger<SchemaProvider> logger) : ISchemaProvider {
    private readonly IKintoneAppMetadataApi _metadataApi = metadataApi;
    private readonly IMetadataConverter _converter = converter;
    private readonly ILogger<SchemaProvider> _logger = logger;
    private string? _domain;

    /// <summary>
    /// Kintoneアプリのスキーマ情報を取得します。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリケーションのID</param>
    /// <returns>取得したスキーマ情報</returns>
    public async Task<KintoneAppSchema> GetSchemaAsync(string domain, string apiToken, int appId) {
        this._logger.LogInformation("Fetching metadata for AppId: {appId}", appId);

        var metadata = await this.GetMetadataAsync(domain, apiToken, appId);
        this._logger.LogInformation("Metadata fetched. Converting to schema...");

        var schema = this._converter.Convert(metadata);
        this._logger.LogInformation("Schema conversion completed.");

        return schema;
    }

    /// <summary>
    /// Kintoneアプリのメタデータを取得します。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン</param>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="appId">アプリケーションのID</param>
    /// <returns>取得したメタデータ情報</returns>
    /// <exception cref="ArgumentNullException">domainが設定されていない場合にスローされます</exception>
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
    /// <param name="subDomain">Kintoneのサブドメイン</param>
    public void SetDomain(string subDomain) => this._domain = $"{subDomain}.cybozu.com";

    /// <summary>
    /// 指定されたバックアップスキーマと現在のスキーマを比較し、差分を取得します。
    /// </summary>
    /// <param name="backupSchema">バックアップスキーマ</param>
    /// <param name="domain">Kintoneのサブドメイン</param>
    /// <param name="appId">アプリケーションのID</param>
    /// <param name="apiToken">APIトークン</param>
    /// <returns>差分情報のリスト</returns>
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