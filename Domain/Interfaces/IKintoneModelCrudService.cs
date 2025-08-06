using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

public interface IKintoneModelCrudService {
    Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    Task<KintoneWriteResult<T>> UpdateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    Task<KintoneDeleteResult> DeleteAsync<T>(IList<T> models, bool validateExistence = true) where T : KintoneModelBase<T>, new();
    Task<IEnumerable<T>> FindAsync<T>(IList<string>? ids = null, string? query = null, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    Task<KintoneWriteResult<T>> SaveAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    Task<KintoneWriteResult<T>> SaveWithRetryAsync<T>(
        IList<T> records,
        bool enableSingleRetryOnError = false,
        bool enableCreateToUpdateRetry = true
    ) where T : KintoneModelBase<T>, new();
}
