using System.Text.Json;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

/// <summary>
/// ローカルの JSON ファイルから Kintone アプリのスキーマ情報を提供するサービス実装
/// </summary>
/// <param name="jsonPath">JSON ファイルのパス</param>
/// <param name="fieldParser">フィールドパーサー</param>
/// <param name="converter">メタデータコンバーター</param>
/// <param name="logger">ロガー</param>
public class LocalJsonSchemaProvider(string jsonPath, IKintoneFieldParser fieldParser, IMetadataConverter converter, ILogger<LocalJsonSchemaProvider> logger) : ISchemaProvider {

    private readonly IKintoneFieldParser _fieldParser = fieldParser;
    private readonly IMetadataConverter _converter = converter;
    private readonly ILogger<LocalJsonSchemaProvider> _logger = logger;
    private readonly string _jsonPath = jsonPath;

    /// <summary>
    /// Kintoneアプリのスキーマ情報を取得します。ローカルの JSON ファイルから読み込みます。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン（未使用）</param>
    /// <param name="apiToken">APIトークン（未使用）</param>
    /// <param name="appId">アプリID</param>
    /// <returns>取得したスキーマ情報</returns>
    public async Task<KintoneAppSchema> GetSchemaAsync(string domain, string apiToken, int appId) {
        this._logger.LogInformation("Loading fields.json from {path}", this._jsonPath);

        var metadata = await this.GetMetadataAsync(string.Empty, string.Empty, appId);
        this._logger.LogInformation("Metadata loaded. Converting to schema...");

        var schema = this._converter.Convert(metadata);
        this._logger.LogInformation("Schema conversion completed.");

        return schema;
    }

    /// <summary>
    /// Kintoneアプリのメタデータを取得します。ローカルの JSON ファイルから読み込みます。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン（未使用）</param>
    /// <param name="apiToken">APIトークン（未使用）</param>
    /// <param name="appId">アプリID</param>
    /// <returns>取得したメタデータ</returns>
    public async Task<KintoneAppMetadata> GetMetadataAsync(string domain, string apiToken, int appId) {
        var json = await File.ReadAllTextAsync(this._jsonPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var properties = root.GetProperty("properties");
        var fields = this._fieldParser.Parse(properties);

        var revisionString = root.GetProperty("revision").GetString();
        if (!int.TryParse(revisionString, out var revision)) {
            revision = 0;
        }

        return new KintoneAppMetadata {
            AppId = appId,
            Revision = revision,
            Fields = fields
        };
    }

    /// <summary>
    /// このプロバイダーはローカルの JSON ファイルからスキーマを提供するため、SetDomain はサポートしていません。
    /// </summary>
    /// <param name="subDomain">未使用</param>
    /// <exception cref="NotSupportedException">このメソッドはサポートされていません。</exception>
    public void SetDomain(string subDomain)
        => throw new NotSupportedException("LocalJsonSchemaProvider does not support SetDomain because it does not connect to Kintone API.");

    /// <summary>
    /// このプロバイダーはローカルの JSON ファイルからスキーマを提供するため、CompareAsync はサポートしていません。
    /// </summary>
    /// <param name="backupSchema">未使用</param>
    /// <param name="domain">未使用</param>
    /// <param name="apiToken">未使用</param>
    /// <param name="appId">未使用</param>
    /// <returns>未使用</returns>
    /// <exception cref="NotSupportedException">このメソッドはサポートされていません。</exception>
    public Task<IReadOnlyList<KintoneMetadataDiff>> CompareAsync(KintoneAppMetadata backupSchema, string domain, string apiToken, int appId)
        => throw new NotSupportedException("LocalJsonSchemaProvider does not support CompareAsync because it only reads local fields.json.");
}
