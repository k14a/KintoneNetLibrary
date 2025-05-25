using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Application.UseCases;

namespace KintoneNetLibrary.Domain.Interfaces;

public interface IKintoneRepository {
    string Domain { get; }
    int AppCode { get; }
    string ApiToken { get; }

    Task<KintoneIndexes> CreateAsync<T>(IEnumerable<T> models) where T : KintoneModelBase;
    // Task<IList<T>> FindAsync<T>(KintoneQuery<T> query) where T : KintoneModelBase, new();
    Task<string?> FindByIDAsync<T>(string id) where T : KintoneModelBase, new();
    Task<string?> FindByIDsAsync<T>(IList<string> ids) where T : KintoneModelBase, new();
    Task<string> FindAllAsync<T>() where T : KintoneModelBase, new();
    Task<string> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase, new();
    Task<string> FindByQueryAsync<T>(string queryStr) where T : KintoneModelBase, new();
    Task<KintoneIndexes> UpdateAsync<T>(IEnumerable<T> models) where T : KintoneModelBase;
    Task<KintoneDeleteResult> DeleteAsync<T>(IEnumerable<T> models) where T : KintoneModelBase;
    Task<KintoneIndexes> SaveAsync<T>(IEnumerable<T> models) where T : KintoneModelBase;
}
