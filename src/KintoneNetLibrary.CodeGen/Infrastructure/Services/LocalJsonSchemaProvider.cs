using System.Text.Json;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

public class LocalJsonSchemaProvider(
    string jsonPath,
    IFieldParser fieldParser,
    IMetadataConverter converter,
    ILogger<LocalJsonSchemaProvider> logger) : ISchemaProvider {

    private readonly IFieldParser _fieldParser = fieldParser;
    private readonly IMetadataConverter _converter = converter;
    private readonly ILogger<LocalJsonSchemaProvider> _logger = logger;
    private readonly string _jsonPath = jsonPath;

    public async Task<KintoneAppSchema> GetSchemaAsync(string domain, string apiToken, int appId) {
        this._logger.LogInformation("Loading fields.json from {path}", this._jsonPath);

        var metadata = await this.GetMetadataAsync(string.Empty, string.Empty, appId);
        this._logger.LogInformation("Metadata loaded. Converting to schema...");

        var schema = this._converter.Convert(metadata);
        this._logger.LogInformation("Schema conversion completed.");

        return schema;
    }

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

    public void SetDomain(string subDomain)
        => throw new NotSupportedException("LocalJsonSchemaProvider does not support SetDomain because it does not connect to Kintone API.");

    public Task<IReadOnlyList<KintoneMetadataDiff>> CompareAsync(KintoneAppMetadata backupSchema, string domain, string apiToken, int appId)
        => throw new NotSupportedException("LocalJsonSchemaProvider does not support CompareAsync because it only reads local fields.json.");
}
