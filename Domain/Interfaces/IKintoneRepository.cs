using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Application.UseCases;

namespace KintoneNetLibrary.Domain.Interfaces;

public interface IKintoneRepository {
    Task<string> CreateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase;
    Task<string> UpdateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase;
    Task<string> DeleteRecordsAsync<T>(IList<T> records) where T : KintoneModelBase;

    Task<string?> FindByIDAsync<T>(T model, string id) where T : KintoneModelBase, new();
    Task<string?> FindByIDsAsync<T>(T model, IList<string> ids) where T : KintoneModelBase, new();
    Task<string?> FindAllAsync<T>(T model) where T : KintoneModelBase, new();
    Task<string?> FindByFieldAsync<T>(T model, string field, string value) where T : KintoneModelBase, new();
    Task<string?> FindByQueryAsync<T>(T model, string queryStr) where T : KintoneModelBase, new();
}
