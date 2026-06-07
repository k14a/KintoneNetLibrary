using System.Linq.Expressions;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneモデルの基本クラス（CRUD操作用）
/// </summary>
public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    /// <summary>
    /// レコードを作成します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneに作成します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>作成結果</returns>
    public async Task<KintoneWriteResult<TSelf>> CreateAsync(IKintoneModelCrudService service, bool enableSingleRetryOnError = false) {
        return await service.CreateAsync([(TSelf)this], enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを更新します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneで更新します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>更新結果</returns>
    public async Task<KintoneWriteResult<TSelf>> UpdateAsync(IKintoneModelCrudService service, bool enableSingleRetryOnError = false) {
        return await service.UpdateAsync([(TSelf)this], enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneから削除します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    public async Task<KintoneDeleteResult> DeleteAsync(IKintoneModelCrudService service, bool validateExistence = true) {
        return await service.DeleteAsync([(TSelf)this], validateExistence);
    }

    /// <summary>
    /// レコードを保存します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneに保存します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    public async Task<KintoneWriteResult<TSelf>> SaveAsync(IKintoneModelCrudService service, bool enableSingleRetryOnError = false) {
        return await service.SaveAsync([(TSelf)this], enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを保存し、必要に応じて再試行します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneに保存し、必要に応じて再試行を行います。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">新規作成から更新への再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    public async Task<KintoneWriteResult<TSelf>> SaveWithRetryAsync(IKintoneModelCrudService service, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        return await service.SaveWithRetryAsync([(TSelf)this], enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

#pragma warning disable CA1000
    /// <summary>
    /// レコードを一括作成します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに一括で作成します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">作成するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>一括作成結果</returns>
    public static async Task<KintoneWriteResult<TSelf>> CreateBulkAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false) {
        return await service.CreateAsync(models, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを単一作成します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに単一で作成します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">作成するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>単一作成結果</returns>
    public static Task<KintoneWriteResult<TSelf>> CreateSingleAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false)
        => RunSingleWriteAsync(models, model => service.CreateAsync([model], enableSingleRetryOnError));

    /// <summary>
    /// レコードを一括更新します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに一括で更新します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">更新するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>一括更新結果</returns>
    public static async Task<KintoneWriteResult<TSelf>> UpdateBulkAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false) {
        return await service.UpdateAsync(models, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを単一更新します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに単一で更新します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">更新するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>単一更新結果</returns>
    public static Task<KintoneWriteResult<TSelf>> UpdateSingleAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false)
        => RunSingleWriteAsync(models, model => service.UpdateAsync([model], enableSingleRetryOnError));

    /// <summary>
    /// レコードを一括削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のレコードIdを指定してKintoneから一括で削除します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="ids">削除するレコードのIdリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    public static async Task<KintoneDeleteResult> DeleteBulkAsync(IKintoneModelCrudService service, IList<string> ids, bool validateExistence = true) {
        return await service.DeleteAsync<TSelf>(ids, validateExistence);
    }

    /// <summary>
    /// レコードを一括削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneから一括で削除します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">削除するモデルのリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    public static async Task<KintoneDeleteResult> DeleteBulkAsync(IKintoneModelCrudService service, IList<TSelf> models, bool validateExistence = true) {
        return await service.DeleteAsync(models, validateExistence);
    }

    /// <summary>
    /// レコードを単一削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のレコードIdを指定してKintoneから単一で削除します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="ids">削除するレコードのIdリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    public static Task<KintoneDeleteResult> DeleteSingleAsync(IKintoneModelCrudService service, IList<string> ids, bool validateExistence = true)
        => RunSingleDeleteAsync(ids, id => service.DeleteAsync<TSelf>([id], validateExistence));

    /// <summary>
    /// レコードを単一削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneから単一で削除します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">削除するモデルのリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    public static Task<KintoneDeleteResult> DeleteSingleAsync(IKintoneModelCrudService service, IList<TSelf> models, bool validateExistence = true)
        => RunSingleDeleteAsync(models, model => service.DeleteAsync([model], validateExistence));

    /// <summary>
    /// レコードを一括保存します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに一括で保存します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>一括保存結果</returns>
    public static async Task<KintoneWriteResult<TSelf>> SaveBulkAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false) {
        return await service.SaveAsync(models, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを単一保存します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに単一で保存します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>単一保存結果</returns>
    public static Task<KintoneWriteResult<TSelf>> SaveSingleAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false)
        => RunSingleWriteAsync(models, model => service.SaveAsync([model], enableSingleRetryOnError));

    /// <summary>
    /// レコードを保存し、必要に応じて再試行します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに保存し、必要に応じて再試行を行います。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">新規作成から更新への再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    public static async Task<KintoneWriteResult<TSelf>> SaveWithRetryBulkAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        return await service.SaveWithRetryAsync(models, enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

    /// <summary>
    /// レコードを保存し、必要に応じて再試行します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに保存し、必要に応じて再試行を行います。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">新規作成から更新への再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    public static Task<KintoneWriteResult<TSelf>> SaveWithRetrySingleAsync(IKintoneModelCrudService service, IList<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true)
        => RunSingleWriteAsync(models, model => service.SaveWithRetryAsync([model], enableSingleRetryOnError, enableCreateToUpdateRetry));

    /// <summary>
    /// レコードをIdで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたIdを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="id">検索するレコードのId</param>
    /// <returns>検索結果のレコード</returns>
    public static async Task<TSelf?> FindByIdAsync(IKintoneModelCrudService service, string id) {
        var result = await service.FindAsync<TSelf>([id], fieldCodes: null);
        return result.FirstOrDefault();
    }

    /// <summary>
    /// レコードをIdのリストで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたIdのリストを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="ids">検索するレコードのIdのリスト</param>
    /// <returns>検索結果のレコードのリスト</returns>
    public static async Task<List<TSelf>> FindByIdsAsync(IKintoneModelCrudService service, IList<string> ids) {
        return [.. await service.FindAsync<TSelf>([.. ids], fieldCodes: null)];
    }

    /// <summary>
    /// レコードをキーで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたキーを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="model">検索するモデルのインスタンス</param>
    /// <param name="keySelector">キーフィールドを指定する式（例: x =&gt; x.Code）</param>
    /// <returns>検索結果のレコード。キー値が null の場合は null。</returns>
    public static async Task<TSelf?> FindByKeyAsync<TKey>(IKintoneModelCrudService service, TSelf model, Expression<Func<TSelf, TKey>> keySelector) {
        var keyValue = keySelector.Compile()(model);
        if (keyValue == null) { return null; }
        var query = new KintoneQuery<TSelf>().Equal(keySelector, keyValue);
        return (await service.FindAsync<TSelf>(query: query.Build())).FirstOrDefault();
    }

    /// <summary>
    /// レコードをキーのリストで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたキーのリストを持つレコードをKintoneから検索します。null 値のキーはスキップされます。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">検索するモデルのリスト</param>
    /// <param name="keySelector">キーフィールドを指定する式（例: x =&gt; x.Code）</param>
    /// <returns>検索結果のレコードのリスト</returns>
    public static async Task<List<TSelf>> FindByKeysAsync<TKey>(IKintoneModelCrudService service, IList<TSelf> models, Expression<Func<TSelf, TKey>> keySelector) {
        var compile = keySelector.Compile();
        var keyValues = models.Select(compile).Where(v => v is not null).Distinct().ToList();
        if (keyValues.Count == 0) { return []; }
        var query = new KintoneQuery<TSelf>().In(keySelector, keyValues);
        return [.. await service.FindAsync<TSelf>(query: query.Build())];
    }

    /// <summary>
    /// レコードをクエリで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたクエリを使用してKintoneからレコードを検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="query">検索クエリ</param>
    /// <returns>検索結果のレコードのリスト</returns>
    public static async Task<List<TSelf>> FindByQueryAsync(IKintoneModelCrudService service, string query) {
        return [.. await service.FindAsync<TSelf>(query: query)];
    }

    /// <summary>
    /// すべてのレコードを検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、Kintoneからすべてのレコードを検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <returns>検索結果のレコードのリスト</returns>
    public static async Task<List<TSelf>> FindAllAsync(IKintoneModelCrudService service) {
        return [.. await service.FindAsync<TSelf>()];
    }

#pragma warning restore CA1000
    /// <summary>
    /// 複数モデルを1件ずつ書き込み操作し、結果をマージして返すヘルパーメソッド
    /// </summary>
    private static async Task<KintoneWriteResult<TSelf>> RunSingleWriteAsync(IList<TSelf> models, Func<TSelf, Task<KintoneWriteResult<TSelf>>> singleOp) {
        var merged = new KintoneWriteResult<TSelf>();
        foreach (var model in models) {
            merged.Merge(await singleOp(model));
        }
        return merged;
    }

    /// <summary>
    /// 複数アイテムを1件ずつ削除操作し、結果をマージして返すヘルパーメソッド
    /// </summary>
    private static async Task<KintoneDeleteResult> RunSingleDeleteAsync<TItem>(IList<TItem> items, Func<TItem, Task<KintoneDeleteResult>> singleOp) {
        var merged = new KintoneDeleteResult();
        foreach (var item in items) {
            merged.Merge(await singleOp(item));
        }
        return merged;
    }

}
