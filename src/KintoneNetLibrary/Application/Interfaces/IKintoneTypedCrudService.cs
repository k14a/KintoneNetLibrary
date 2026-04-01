using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// KintoneモデルのCRUD操作を提供するサービスインターフェイスです。
/// </summary>
public interface IKintoneTypedCrudService<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// Kintoneモデルのレコードを作成します。
    /// </summary>
    /// <param name="records">作成するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>作成結果の情報</returns>
    Task<KintoneWriteResult<T>> CreateAsync(IList<T> records, bool enableSingleRetryOnError = false);
    /// <summary>
    /// Kintoneモデルのレコードを検索します。
    /// </summary>
    /// <param name="ids">検索するレコードのIDリスト（オプション）</param>
    /// <param name="query">検索クエリ（オプション）</param>
    /// <param name="kintoneQuery">Kintoneクエリオブジェクト（オプション）</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>検索結果のレコードのリスト</returns>
    /// <exception cref="KintoneException"></exception>
    Task<IEnumerable<T>> FindAsync(IList<string>? ids = null, string? query = null, KintoneQuery<T>? kintoneQuery = null, IList<string>? fieldCodes = null);
    /// <summary>
    /// Kintoneモデルのレコードを更新します。
    /// </summary>
    /// <param name="records">更新するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>更新結果の情報</returns>
    Task<KintoneWriteResult<T>> UpdateAsync(IList<T> records, bool enableSingleRetryOnError = false);
    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <param name="ids">削除するレコードのIDリスト</param>
    /// <param name="validateExistence">存在確認を行うかどうか</param>
    /// <returns>削除結果の情報</returns>
    Task<KintoneDeleteResult> DeleteAsync(IList<string> ids, bool validateExistence = true);
    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <param name="models">削除するレコードのモデルリスト</param>
    /// <param name="validateExistence">存在確認を行うかどうか</param>
    /// <returns>削除結果の情報</returns>
    Task<KintoneDeleteResult> DeleteAsync(IList<T> models, bool validateExistence = true);
    /// <summary>
    /// Kintoneモデルのレコードを保存します。
    /// </summary>
    /// <param name="records">保存するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>保存結果の情報</returns>
    Task<KintoneWriteResult<T>> SaveAsync(IList<T> records, bool enableSingleRetryOnError = false);
    /// <summary>
    /// Kintoneモデルのレコードを保存します。作成に失敗したレコードは更新として再試行されます。
    /// </summary>
    /// <param name="records">保存するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">作成に失敗した場合に更新として再試行するかどうか</param>
    /// <returns>保存結果の情報</returns>
    Task<KintoneWriteResult<T>> SaveWithRetryAsync(IList<T> records, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true);
}