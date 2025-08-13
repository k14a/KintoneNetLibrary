using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Application.UseCases;

namespace KintoneNetLibrary.Domain.Interfaces;

public interface IKintoneRepository {
    Task<string> CreateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();
    Task<string> UpdateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();
    Task<string> DeleteRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();

    Task<string?> FindByIDAsync<T>(T model, string id) where T : KintoneModelBase<T>, new();
    Task<string?> FindByIDsAsync<T>(T model, IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    Task<string?> FindAllAsync<T>(T model, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    Task<string?> FindByFieldAsync<T>(T model, string field, string value) where T : KintoneModelBase<T>, new();
    Task<string?> FindByQueryAsync<T>(T model, string queryStr) where T : KintoneModelBase<T>, new();
    Task<string> UploadFileAsync<T>(T model, FileInfo file) where T : KintoneModelBase<T>, new();
    Task<byte[]> DownloadFileAsync<T>(T model, string fileKey) where T : KintoneModelBase<T>, new();
}
