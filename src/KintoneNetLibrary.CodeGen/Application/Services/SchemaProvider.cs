using Microsoft.Extensions.Logging;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Infrastructure.Api;

namespace KintoneNetLibrary.CodeGen.Application.Services;

/// <summary>
/// スキーマプロバイダーサービス
/// </summary>
/// <param name="httpClientFactory"></param>
/// <param name="logger"></param>
public class SchemaProvider(IHttpClientFactory httpClientFactory, ILogger<SchemaProvider> logger) : ISchemaProvider {
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<SchemaProvider> _logger = logger;
    private string? _domain;

    /// <summary>
    /// Kintoneアプリのスキーマ情報を取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    public async Task<KintoneAppSchema> GetSchemaAsync(int appId, string apiToken) {
        this._logger.LogInformation("Fetching metadata for AppId: {appId}", appId);

        var metadata = await this.GetMetadataAsync(appId, apiToken);
        this._logger.LogInformation("Metadata fetched. Converting to schema...");

        var schema = this.ConvertMetadataToSchema(metadata);
        this._logger.LogInformation("Schema conversion completed.");

        return schema;
    }

    /// <summary>
    /// Kintoneアプリのメタデータを取得します。
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="apiToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<KintoneAppMetadata> GetMetadataAsync(int appId, string apiToken) {
        if (this._domain is null) {
            var message = "domainが設定されていません。";
            throw new ArgumentNullException(message);
        }

        var access = new ApiTokenAccess(this._domain, apiToken);
        var httpClient = this._httpClientFactory.CreateClient();
        var metaApi = new KintoneAppMetadataApi(access, httpClient);

        return await metaApi.GetAppMetadataAsync(appId, apiToken);
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
            Fields = [.. metadata.Fields.Where(f => f.Type != KintoneFieldType.SubTable).Select(f => new KintoneFieldSchema {
                FieldCode = f.Code,
                Label = f.Label,
                FieldType = f.Type,
                Required = f.Required,
                Options = f.Options == null ? [] : [.. f.Options], // ドロップダウンなど
                // 必要に応じて追加
            })],
            SubTables = [.. metadata.Fields.Where(st => st.Type == KintoneFieldType.SubTable).Select(st => new KintoneSubTableSchema {
                FieldCode = st.Code,
                Label = st.Label,
                Fields = [.. st.SubFields!.Select(sf => new KintoneFieldSchema {
                    FieldCode = sf.Code,
                    Label = sf.Label,
                    FieldType = sf.Type,
                    Required = sf.Required,
                    Options = sf.Options == null ? [] : [.. sf.Options], // ドロップダウンなど
                    // 必要に応じて追加
                })]
            })]
        };
    }
}