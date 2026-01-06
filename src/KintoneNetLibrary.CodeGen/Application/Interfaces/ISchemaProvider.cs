using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface ISchemaProvider {
    Task<KintoneAppSchema> GetSchemaAsync(int appId, string apiToken);
    void SetDomain(string subDomain);
    Task<KintoneAppMetadata> GetMetadataAsync(int appId, string apiToken)
}