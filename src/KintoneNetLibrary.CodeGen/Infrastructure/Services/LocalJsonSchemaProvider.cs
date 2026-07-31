using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

/// <summary>
/// ローカルの JSON ファイルから Kintone アプリのスキーマ情報を提供するサービス実装
/// </summary>
/// <param name="fieldsJsonPath">fields.json のパス。同じディレクトリに layout.json が存在することを前提とする</param>
/// <param name="metadataLoader">メタデータローダー</param>
/// <param name="converter">メタデータコンバーター</param>
/// <param name="logger">ロガー</param>
public class LocalJsonSchemaProvider(string fieldsJsonPath, IMetadataLoader metadataLoader, IMetadataConverter converter, ILogger<LocalJsonSchemaProvider> logger) : ISchemaProvider {

    private readonly IMetadataLoader _metadataLoader = metadataLoader;
    private readonly IMetadataConverter _converter = converter;
    private readonly ILogger<LocalJsonSchemaProvider> _logger = logger;
    private readonly string _fieldsJsonPath = fieldsJsonPath;

    /// <summary>
    /// Kintoneアプリのスキーマ情報を取得します。ローカルの JSON ファイルから読み込みます。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン（未使用）</param>
    /// <param name="apiToken">APIトークン（未使用）</param>
    /// <param name="appId">アプリId</param>
    /// <returns>取得したスキーマ情報</returns>
    public async Task<KintoneAppSchema> GetSchemaAsync(string domain, string apiToken, int appId) {
        this._logger.LogInformation("Loading fields.json from {path}", this._fieldsJsonPath);

        var metadata = await this.GetMetadataAsync(string.Empty, string.Empty, appId);
        this._logger.LogInformation("Metadata loaded. Converting to schema...");

        var schema = this._converter.Convert(metadata);
        this._logger.LogInformation("Schema conversion completed.");

        return schema;
    }

    /// <summary>
    /// Kintoneアプリのメタデータを取得します。ローカルの JSON ファイルから読み込みます。
    /// fields.json と同じディレクトリに存在する layout.json も併せて読み込みます。
    /// </summary>
    /// <param name="domain">Kintoneのサブドメイン（未使用）</param>
    /// <param name="apiToken">APIトークン（未使用）</param>
    /// <param name="appId">アプリId</param>
    /// <returns>取得したメタデータ</returns>
    public async Task<KintoneAppMetadata> GetMetadataAsync(string domain, string apiToken, int appId) {
        var directory = Path.GetDirectoryName(this._fieldsJsonPath);
        var layoutJsonPath = Path.Combine(string.IsNullOrEmpty(directory) ? "." : directory, "layout.json");

        if (!File.Exists(layoutJsonPath)) {
            throw new FileNotFoundException(
                $"layout.json が見つかりません。fields.json と同じディレクトリに layout.json を配置してください。想定パス: {layoutJsonPath}",
                layoutJsonPath);
        }

        this._logger.LogInformation("Loading layout.json from {path}", layoutJsonPath);

        var metadata = await this._metadataLoader.LoadAsync(this._fieldsJsonPath, layoutJsonPath);
        metadata.AppId = appId;

        return metadata;
    }

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
