using System.Linq.Expressions;
using System.Reflection;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneモデルの基本クラス（CRUD操作用）
/// </summary>
/// <typeparam name="TSelf"></typeparam>
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
    /// このメソッドは、複数のレコードIDを指定してKintoneから一括で削除します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="ids">削除するレコードのIDリスト</param>
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
    /// このメソッドは、複数のレコードIDを指定してKintoneから単一で削除します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="ids">削除するレコードのIDリスト</param>
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
    /// レコードをIDで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたIDを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="id">検索するレコードのID</param>
    /// <returns>検索結果のレコード</returns>
    public static async Task<TSelf?> FindByIDAsync(IKintoneModelCrudService service, string id) {
        var result = await service.FindAsync<TSelf>([id], fieldCodes: null);
        return result.FirstOrDefault();
    }

    /// <summary>
    /// レコードをIDのリストで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたIDのリストを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="ids">検索するレコードのIDのリスト</param>
    /// <returns>検索結果のレコードのリスト</returns>
    public static async Task<List<TSelf>> FindByIDsAsync(IKintoneModelCrudService service, IList<string> ids) {
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
    /// <returns>検索結果のレコード</returns>
    public static async Task<TSelf?> FindByKeyAsync(IKintoneModelCrudService service, TSelf model) {
        KintoneModelValidator.ValidateKeyIntegrity(model);

        var keyProp = typeof(TSelf)
            .GetProperties()
            .First(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        var keyValue = keyProp.GetValue(model);
        if (keyValue == null) { return null; }

        var keyType = keyProp.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(keyType) ?? keyType;

        var method = typeof(KintoneModelBase<TSelf>)
            .GetMethod("CreateFieldSelector", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(underlyingType);

        var fieldSelector = method.Invoke(null, [keyProp]);
        var equalMethod = typeof(KintoneQuery<TSelf>)
            .GetMethod("Equal")!
            .MakeGenericMethod(underlyingType);

        var query = new KintoneQuery<TSelf>();
        query = (KintoneQuery<TSelf>)equalMethod.Invoke(query, [fieldSelector, keyValue])!;

        return (await service.FindAsync<TSelf>(query: query.Build())).FirstOrDefault();
    }

    /// <summary>
    /// レコードをキーのリストで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたキーのリストを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="service">CRUDサービスのインスタンス</param>
    /// <param name="models">検索するモデルのリスト</param>
    /// <returns>検索結果のレコードのリスト</returns>
    public static async Task<List<TSelf>> FindByKeysAsync(IKintoneModelCrudService service, IList<TSelf> models) {
        var modelList = models.ToList();
        foreach (var model in modelList) {
            KintoneModelValidator.ValidateKeyIntegrity(model);
        }
        KintoneModelValidator.ValidateKeyValueUniqueness(modelList);

        var keyProp = typeof(TSelf)
            .GetProperties()
            .First(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        var keyValues = modelList
            .Select(m => keyProp.GetValue(m))
            .Where(v => v != null)
            .Distinct()
            .ToList();

        var keyType = keyProp.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(keyType) ?? keyType;

        var method = typeof(KintoneModelBase<TSelf>)
            .GetMethod("CreateFieldSelector", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(underlyingType);

        var fieldSelector = (LambdaExpression)method.Invoke(null, [keyProp])!;
        var targetType = typeof(Expression<>)
            .MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TSelf), underlyingType));

        var typedSelector = ConvertExpression(fieldSelector, targetType);

        var inMethod = typeof(KintoneQuery<TSelf>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "In" && m.IsGenericMethod)
            .MakeGenericMethod(underlyingType);

        var castMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Cast" && m.GetParameters().Length == 1)
            .MakeGenericMethod(underlyingType);

        var castedValues = castMethod.Invoke(null, [keyValues]);

        var query = new KintoneQuery<TSelf>();
        query = (KintoneQuery<TSelf>)inMethod.Invoke(query, [typedSelector, castedValues])!;

        var result = await service.FindAsync<TSelf>(query: query.Build());
        return [.. result];
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

    /// <summary>
    /// レコードのフィールドセレクターを作成します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたプロパティ情報を使用してフィールドセレクターを作成します。
    /// </remarks>
    /// <typeparam name="TValue">フィールドの値の型</typeparam>
    /// <param name="prop">プロパティ情報</param>
    /// <returns>フィールドセレクターの式</returns>
    private static Expression<Func<TSelf, TValue>> CreateFieldSelector<TValue>(PropertyInfo prop) {
        var param = Expression.Parameter(typeof(TSelf), "x");
        var body = Expression.Property(param, prop.Name);
        return Expression.Lambda<Func<TSelf, TValue>>(body, param);
    }

    /// <summary>
    /// 指定された式をターゲット型に変換します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定された式をターゲット型の式に変換します。
    /// </remarks>
    /// <param name="source">変換元の式</param>
    /// <param name="targetType">変換先の型</param>
    /// <returns>変換された式</returns>
    private static object ConvertExpression(LambdaExpression source, Type targetType) {
        var method = typeof(Expression)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Lambda" && m.IsGenericMethod && m.GetParameters().Length == 2);

        var delegateType = targetType.GetGenericArguments()[0]; // Func<TSelf, TValue>
        var genericMethod = method.MakeGenericMethod(delegateType);

        var parameters = new object[] { source.Body, source.Parameters.ToArray() };
        return genericMethod.Invoke(null, parameters)!;
    }
}
