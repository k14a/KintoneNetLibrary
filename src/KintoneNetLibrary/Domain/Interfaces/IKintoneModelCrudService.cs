using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

// コメントは日本語で記述
/// <summary>
/// Kintoneのモデルに対するCRUD操作を定義するインターフェース
/// </summary>
public interface IKintoneModelCrudService {
    /// <summary>
    /// 新しいレコードを作成します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// 既存のレコードを更新します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> UpdateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを削除します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="models"></param>
    /// <param name="validateExistence"></param>
    /// <returns></returns>
    Task<KintoneDeleteResult> DeleteAsync<T>(IList<T> models, bool validateExistence = true) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを削除します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ids"></param>
    /// <param name="validateExistence"></param>
    /// <returns></returns>
    Task<KintoneDeleteResult> DeleteAsync<T>(IList<string> ids, bool validateExistence = true) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを検索します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ids"></param>
    /// <param name="query"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<IEnumerable<T>> FindAsync<T>(IList<string>? ids = null, string? query = null, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新） 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> SaveAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <param name="enableCreateToUpdateRetry"></param>
    /// <returns></returns>
    Task<KintoneWriteResult<T>> SaveWithRetryAsync<T>(
        IList<T> records,
        bool enableSingleRetryOnError = false,
        bool enableCreateToUpdateRetry = true
    ) where T : KintoneModelBase<T>, new();
}
