using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.Infrastructure.Services;

/// <summary>
/// Kintoneのモデルに対するCRUD操作を提供するサービス
/// </summary>
/// <param name="provider"></param>
public class KintoneModelCrudService(IServiceProvider provider) : IKintoneModelCrudService {
    private readonly IServiceProvider _provider = provider;

    /// <summary>
    /// 新しいレコードを作成します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    public Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.CreateAsync(records, enableSingleRetryOnError);
    }

    /// <summary>
    /// 既存のレコードを更新します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="models"></param>
    /// <param name="validateExistence"></param>
    /// <returns></returns>
    public Task<KintoneDeleteResult> DeleteAsync<T>(IList<T> models, bool validateExistence = true) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.DeleteAsync(models, validateExistence);
    }

    /// <summary>
    /// レコードを削除します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ids"></param>
    /// <param name="validateExistence"></param>
    /// <returns></returns>
    public Task<KintoneDeleteResult> DeleteAsync<T>(IList<string> ids, bool validateExistence = true) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.DeleteAsync(ids, validateExistence);
    }

    /// <summary>
    /// レコードを検索します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <returns></returns>
    public Task<IEnumerable<T>> FindAsync<T>(string? query = null) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.FindAsync(query: query);
    }

    /// <summary>
    /// レコードを検索します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ids"></param>
    /// <param name="query"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    public Task<IEnumerable<T>> FindAsync<T>(IList<string>? ids = null, string? query = null, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.FindAsync(ids, query, fieldCodes);
    }

    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    public Task<KintoneWriteResult<T>> SaveAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.SaveAsync(records, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを保存します（存在しない場合は作成、存在する場合は更新）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <param name="enableCreateToUpdateRetry"></param>
    /// <returns></returns>
    public Task<KintoneWriteResult<T>> SaveWithRetryAsync<T>(IList<T> records, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.SaveWithRetryAsync(records, enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

    /// <summary>
    /// 既存のレコードを更新します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="enableSingleRetryOnError"></param>
    /// <returns></returns>
    public Task<KintoneWriteResult<T>> UpdateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        // Typed CRUD を解決して委譲
        var typed = this._provider.GetRequiredService<IKintoneTypedCrudService<T>>();
        return typed.UpdateAsync(records, enableSingleRetryOnError);
    }
}
