using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface ISchemaProvider {
    Task<KintoneAppSchema> GetSchemaAsync(int appId, string apiToken);
    void SetDomain(string subDomain);
}