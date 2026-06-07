using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// Kintoneのモデルに対するCRUD操作を定義するインターフェース
/// </summary>
public interface IKintoneModelCrudService {
    /// <summary>
    /// 新しいレコードを作成します
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="records">作成するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一のリトライを有効にするかどうか</param>
    /// <returns>作成結果を含むKintoneWriteResult</returns>
    Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// 既存のレコードを更新します
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="records">更新するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一のリトライを有効にするかどうか</param>
    /// <returns>更新結果を含むKintoneWriteResult</returns>
    Task<KintoneWriteResult<T>> UpdateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを削除します
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="models">削除するレコードのリスト</param>
    /// <param name="validateExistence">存在確認を行うかどうか</param>
    /// <returns>削除結果を含むKintoneDeleteResult</returns>
    Task<KintoneDeleteResult> DeleteAsync<T>(IList<T> models, bool validateExistence = true) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを削除します
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="ids">削除するレコードのIdリスト</param>
    /// <param name="validateExistence">存在確認を行うかどうか</param>
    /// <returns>削除結果を含むKintoneDeleteResult</returns>
    Task<KintoneDeleteResult> DeleteAsync<T>(IList<string> ids, bool validateExistence = true) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを検索します
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="ids">検索するレコードのIdリスト</param>
    /// <param name="query">検索クエリ</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>検索結果を含むレコードのリスト</returns>
    Task<IEnumerable<T>> FindAsync<T>(IList<string>? ids = null, string? query = null, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新）
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="records">保存するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一のリトライを有効にするかどうか</param>
    /// <returns>保存結果を含むKintoneWriteResult</returns>
    Task<KintoneWriteResult<T>> SaveAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新）
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="records">保存するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一のリトライを有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">作成から更新へのリトライを有効にするかどうか</param>
    /// <returns>保存結果を含むKintoneWriteResult</returns>
    Task<KintoneWriteResult<T>> SaveWithRetryAsync<T>(
        IList<T> records,
        bool enableSingleRetryOnError = false,
        bool enableCreateToUpdateRetry = true
    ) where T : KintoneModelBase<T>, new();
}
