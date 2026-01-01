using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// KintoneモデルのCRUD操作を提供するサービスインターフェイスです。
/// </summary>
public interface IKintoneModelCrudService<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// Kintoneモデルのレコードを作成します。
    /// </summary>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> CreateAsync(IList<T> records, bool enableSingleRetryOnError = false);
    /// <summary>
    /// Kintoneモデルのレコードを検索します。
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="query"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    /// <exception cref="KintoneException"></exception>
    Task<IEnumerable<T>> FindAsync(IList<string>? ids = null, string? query = null, IList<string>? fieldCodes = null);
    /// <summary>
    /// Kintoneモデルのレコードを更新します。
    /// </summary>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> UpdateAsync(IList<T> records, bool enableSingleRetryOnError = false);
    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="validateExistence"></param>
    /// <returns></returns>
    Task<KintoneDeleteResult> DeleteAsync(IList<string> ids, bool validateExistence = true);
    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <param name="models"></param>
    /// <param name="validateExistence"></param>
    /// <returns></returns>
    Task<KintoneDeleteResult> DeleteAsync(IList<T> models, bool validateExistence = true);
    /// <summary>
    /// Kintoneモデルのレコードを保存します。
    /// </summary>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> SaveAsync(IList<T> records, bool enableSingleRetryOnError = false);
    /// <summary>
    /// Kintoneモデルのレコードを保存します。作成に失敗したレコードは更新として再試行されます。
    /// </summary>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <param name="enableCreateToUpdateRetry"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> SaveWithRetryAsync(IList<T> records, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true);
}