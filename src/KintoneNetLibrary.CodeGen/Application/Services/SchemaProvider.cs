using Microsoft.Extensions.Logging;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Infrastructure.Api;
using System.Collections.Specialized;

namespace KintoneNetLibrary.CodeGen.Application.Services;

public class SchemaProvider(IHttpClientFactory httpClientFactory, ILogger<SchemaProvider> logger) : ISchemaProvider {
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<SchemaProvider> _logger = logger;
    private string? _domain;

    public async Task<KintoneAppSchema> GetSchemaAsync(int appId, string apiToken) {
        this._logger.LogInformation("Fetching metadata for AppId: {appId}", appId);

        if(this._domain is null) { throw new ArgumentNullException(nameof(this._domain)); }

        var access = new ApiTokenAccess(this._domain, apiToken);
        var httpClient = this._httpClientFactory.CreateClient();
        var metaApi = new KintoneAppMetadataApi(access, httpClient);

        var metadata = await metaApi.GetAppMetadataAsync(appId, apiToken);
        this._logger.LogInformation("Metadata fetched. Converting to schema...");

        var schema = this.ConvertMetadataToSchema(metadata);
        this._logger.LogInformation("Schema conversion completed.");

        return schema;
    }

    public void SetDomain(string subDomain) {
        this._domain = $"https://{subDomain}.cybozu.com";
    }

    public Task<KintoneAppMetadata> GetMetadataAsync(int appId, string apiToken) {
        throw new NotImplementedException();
    }

    private KintoneAppSchema ConvertMetadataToSchema(KintoneAppMetadata metadata) {
        return new KintoneAppSchema {
            AppId = metadata.AppId,
            Revision = metadata.Revision,
            // AppName = metadata.AppName,
            Fields = metadata.Fields.Select(f => new KintoneFieldSchema {
                FieldCode = f.Code,
                Label = f.Label,
                FieldType = f.Type,
                Required = f.Required,
                // NoLabel = f.NoLabel,
                Options = f.Options == null ? [] : [.. f.Options], // ドロップダウンなど
                // 必要に応じて追加
            }).ToList()
        };
    }
}