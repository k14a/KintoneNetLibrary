using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.Infrastructure.Services;

/// <summary>
/// Kintoneのモデルに対するCRUD操作を提供するサービス
/// </summary>
/// <param name="provider">依存関係の解決に使用するサービスプロバイダー</param>
public class KintoneModelCrudService(IServiceProvider provider) : IKintoneModelCrudService {
    private readonly IServiceProvider _provider = provider;

    /// <summary>
    /// 新しいレコードを作成します
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">作成対象のレコードリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一リトライを有効にするかどうか</param>
    /// <returns>作成結果のインデックス情報</returns>
    public Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.CreateAsync(records, enableSingleRetryOnError);
    }

    /// <summary>
    /// 既存のレコードを削除します
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="models">削除対象のモデルリスト</param>
    /// <param name="validateExistence">存在確認を行うかどうか</param>
    /// <returns>削除されたレコードの情報</returns>
    public Task<KintoneDeleteResult> DeleteAsync<T>(IList<T> models, bool validateExistence = true) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.DeleteAsync(models, validateExistence);
    }

    /// <summary>
    /// レコードを削除します
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="ids">削除対象のレコードIdリスト</param>
    /// <param name="validateExistence">存在確認を行うかどうか</param>
    /// <returns>削除結果のインデックス情報</returns>
    public Task<KintoneDeleteResult> DeleteAsync<T>(IList<string> ids, bool validateExistence = true) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.DeleteAsync(ids, validateExistence);
    }

    /// <summary>
    /// レコードを検索します
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="query">検索対象のクエリ文字列</param>
    /// <returns>検索結果のモデルリスト</returns>
    public Task<IEnumerable<T>> FindAsync<T>(string? query = null) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.FindAsync(query: query);
    }

    /// <summary>
    /// レコードを検索します
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="ids">検索対象のレコードIdリスト</param>
    /// <param name="query">検索対象のクエリ文字列</param>
    /// <param name="fieldCodes">取得対象のフィールドコードリスト</param>
    /// <returns>検索結果のモデルリスト</returns>
    public Task<IEnumerable<T>> FindAsync<T>(IList<string>? ids = null, string? query = null, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.FindAsync(ids, query, fieldCodes: fieldCodes);
    }

    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新）
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">保存対象のモデルリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一リトライを有効にするかどうか</param>
    /// <returns>保存結果のインデックス情報</returns>
    public Task<KintoneWriteResult<T>> SaveAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.SaveAsync(records, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新）
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">保存対象のモデルリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一リトライを有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">作成から更新へのリトライを有効にするかどうか</param>
    /// <returns>保存結果のインデックス情報</returns>
    public Task<KintoneWriteResult<T>> SaveWithRetryAsync<T>(IList<T> records, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.SaveWithRetryAsync(records, enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

    /// <summary>
    /// 既存のレコードを更新します
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">更新対象のモデルリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一リトライを有効にするかどうか</param>
    /// <returns>更新結果のインデックス情報</returns>
    public Task<KintoneWriteResult<T>> UpdateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.UpdateAsync(records, enableSingleRetryOnError);
    }
}
