using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    /// <summary>
    /// CRUDサービスのインスタンス
    /// </summary>
    /// <remarks>
    /// このプロパティは、CRUD操作を実行するためのサービスインスタンスを提供します。
    /// </remarks>
    protected IKintoneModelCrudService Service => KintoneServiceLocator.Resolve<IKintoneModelCrudService>();

    /// <summary>
    /// レコードを作成します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneに作成します。
    /// </remarks>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>作成結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneWriteResult<TSelf>> CreateAsync(bool enableSingleRetryOnError = false) {
        return await this.Service.CreateAsync([(TSelf)this], enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを更新します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneで更新します。
    /// </remarks>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>更新結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneWriteResult<TSelf>> UpdateAsync(bool enableSingleRetryOnError = false) {
        return await this.Service.UpdateAsync([(TSelf)this], enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを削除します。  
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneから削除します。
    /// </remarks>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneDeleteResult> DeleteAsync(bool validateExistence = true) {
        return await this.Service.DeleteAsync([(TSelf)this], validateExistence);
    }

    /// <summary>
    /// レコードを保存します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneに保存します。
    /// </remarks>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneWriteResult<TSelf>> SaveAsync(bool enableSingleRetryOnError = false) {
        return await this.Service.SaveAsync([(TSelf)this], enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを保存し、必要に応じて再試行します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、現在のモデルインスタンスをKintoneに保存し、必要に応じて再試行を行います。
    /// </remarks>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">新規作成から更新への再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneWriteResult<TSelf>> SaveWithRetryAsync(bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        return await this.Service.SaveWithRetryAsync([(TSelf)this], enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

    /// <summary>
    /// レコードを一括作成します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに一括で作成します。
    /// </remarks>
    /// <param name="models">作成するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>一括作成結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> CreateBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.CreateAsync(models, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを単一作成します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに単一で作成します。
    /// </remarks>
    /// <param name="models">作成するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>単一作成結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> CreateSingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.CreateAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }

    /// <summary>
    /// レコードを一括更新します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに一括で更新します。
    /// </remarks>
    /// <param name="models">更新するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>一括更新結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> UpdateBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.UpdateAsync(models, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを単一更新します。  
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに単一で更新します。
    /// </remarks>
    /// <param name="models">更新するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>単一更新結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> UpdateSingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.UpdateAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }

    /// <summary>
    /// レコードを一括削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のレコードIDを指定してKintoneから一括で削除します。
    /// </remarks>
    /// <param name="ids">削除するレコードのIDリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneDeleteResult> DeleteBulkAsync(IList<string> ids, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.DeleteAsync<TSelf>(ids, validateExistence);
    }

    /// <summary>
    /// レコードを単一削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneから単一で削除します。
    /// </remarks>
    /// <param name="models">削除するモデルのリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneDeleteResult> DeleteBulkAsync(IList<TSelf> models, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.DeleteAsync(models, validateExistence);
    }

    /// <summary>
    /// レコードを単一削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のレコードIDを指定してKintoneから単一で削除します。
    /// </remarks>
    /// <param name="ids">削除するレコードのIDリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneDeleteResult> DeleteSingleAsync(IList<string> ids, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneDeleteResult();

        foreach (var id in ids) {
            var result = await service.DeleteAsync<TSelf>([id], validateExistence);
            mergedResult.Succeeded.AddRange(result.Succeeded);
            mergedResult.Failed.AddRange(result.Failed);
        }

        return mergedResult;
    }

    /// <summary>
    /// レコードを単一削除します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneから単一で削除します。
    /// </remarks>
    /// <param name="models">削除するモデルのリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneDeleteResult> DeleteSingleAsync(IList<TSelf> models, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneDeleteResult();

        foreach (var model in models) {
            var result = await service.DeleteAsync([model], validateExistence);
            mergedResult.Succeeded.AddRange(result.Succeeded);
            mergedResult.Failed.AddRange(result.Failed);
        }

        return mergedResult;
    }

    /// <summary>
    /// レコードを一括保存します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに一括で保存します。
    /// </remarks>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>一括保存結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> SaveBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.SaveAsync(models, enableSingleRetryOnError);
    }

    /// <summary>
    /// レコードを単一保存します。
    /// </summary>
    /// <remarks>   
    /// このメソッドは、複数のモデルインスタンスをKintoneに単一で保存します。
    /// </remarks>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>単一保存結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> SaveSingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.SaveAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }

    /// <summary>
    /// レコードを保存し、必要に応じて再試行します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに保存し、必要に応じて再試行を行います。
    /// </remarks>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">新規作成から更新への再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> SaveWithRetryBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.SaveWithRetryAsync(models, enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

    /// <summary>
    /// レコードを保存し、必要に応じて再試行します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数のモデルインスタンスをKintoneに保存し、必要に応じて再試行を行います。
    /// </remarks>
    /// <param name="models">保存するモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">新規作成から更新への再試行を有効にするかどうか</param>
    /// <returns>保存結果</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<KintoneWriteResult<TSelf>> SaveWithRetrySingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.SaveWithRetryAsync([model], enableSingleRetryOnError, enableCreateToUpdateRetry);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }

    /// <summary>
    /// レコードをIDで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたIDを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="id">検索するレコードのID</param>
    /// <returns>検索結果のレコード</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<TSelf?> FindByIDAsync(string id) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var result = await service.FindAsync<TSelf>([id], fieldCodes: null);
        return result.FirstOrDefault();
    }

    /// <summary>
    /// レコードをIDのリストで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたIDのリストを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="ids">検索するレコードのIDのリスト</param>
    /// <returns>検索結果のレコードのリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    public static async Task<List<TSelf>> FindByIDsAsync(IList<string> ids) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return [.. await service.FindAsync<TSelf>([.. ids], fieldCodes: null)];
    }

    /// <summary>
    /// レコードをキーで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたキーを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="model">検索するモデルのインスタンス</param>
    /// <returns>検索結果のレコード</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="ArgumentException">キーが設定されていない場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">キーの値が複数存在する場合にスローされます。</exception>
    public static async Task<TSelf?> FindByKeyAsync(TSelf model) {
        KintoneModelValidator.ValidateKeyIntegrity(model);

        var keyProp = typeof(TSelf)
            .GetProperties()
            .First(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        var keyValue = keyProp.GetValue(model);
        if (keyValue == null) { return null; }

        var keyTYpe = keyProp.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(keyTYpe) ?? keyTYpe;

        var method = typeof(KintoneModelBase<TSelf>)
            .GetMethod("CreateFieldSelector", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(underlyingType);

        var fieldSelector = method.Invoke(null, [keyProp]);
        var equalMethod = typeof(KintoneQuery<TSelf>)
            .GetMethod("Equal")!
            .MakeGenericMethod(underlyingType);

        var query = new KintoneQuery<TSelf>();
        query = (KintoneQuery<TSelf>)equalMethod.Invoke(query, [fieldSelector, keyValue])!;

        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return (await service.FindAsync<TSelf>(query: query.Build())).FirstOrDefault();
    }

    /// <summary>
    /// レコードをキーのリストで検索します。
    /// </summary>
    /// <remarks>   
    /// このメソッドは、指定されたキーのリストを持つレコードをKintoneから検索します。
    /// </remarks>
    /// <param name="models">検索するモデルのリスト</param>
    /// <returns>検索結果のレコードのリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="ArgumentException">キーが設定されていない場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">キーの値が複数存在する場合にスローされます。</exception>
    public static async Task<List<TSelf>> FindByKeysAsync(IList<TSelf> models) {
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

        var keyTYpe = keyProp.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(keyTYpe) ?? keyTYpe;

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

        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var result = await service.FindAsync<TSelf>(query: query.Build());
        return [.. result];
    }

    /// <summary>
    /// レコードをクエリで検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたクエリを使用してKintoneからレコードを検索します。
    /// </remarks>
    /// <param name="query">検索クエリ</param>  
    /// <returns>検索結果のレコードのリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="ArgumentException">クエリが無効な場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">クエリの結果が複数存在する場合にスローされます。</exception>
    /// <exception cref="NotSupportedException">クエリがサポートされていない場合にスローされます。</exception>
    /// <exception cref="TimeoutException">クエリの実行がタイムアウトした場合にスローされます。</exception>
    /// <exception cref="Exception">その他のエラーが発生した場合にスローされます。</exception>
    /// <exception cref="AggregateException">複数の例外が発生した場合にスローされます。</exception>
    public static async Task<List<TSelf>> FindByQueryAsync(string query) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return [.. await service.FindAsync<TSelf>(query: query)];
    }

    /// <summary>
    /// すべてのレコードを検索します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、Kintoneからすべてのレコードを検索します。
    /// </remarks>
    /// <returns>検索結果のレコードのリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="TimeoutException">検索の実行がタイムアウトした場合にスローされます。</exception>
    /// <exception cref="Exception">その他のエラーが発生した場合にスローされます。</exception>
    /// <exception cref="AggregateException">複数の例外が発生した場合にスローされます。</exception>
    public static async Task<List<TSelf>> FindAllAsync() {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return [.. await service.FindAsync<TSelf>()];
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
    /// <exception cref="ArgumentNullException">プロパティ情報がnullの場合にスローされます。</exception>
    /// <exception cref="ArgumentException">プロパティがKintoneモデルのプロパティでない場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">プロパティがKintoneモデルのプロパティでない場合にスローされます。</exception>
    /// <exception cref="NotSupportedException">プロパティの型がサポートされていない場合にスローされます。</exception>
    /// <exception cref="TimeoutException">フィールドセレクターの作成がタイムアウトした場合にスローされます。</exception>
    /// <exception cref="Exception">その他のエラーが発生した場合にスローされます。</exception>
    /// <exception cref="AggregateException">複数の例外が発生した場合にスローされます。</exception>
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
    /// <exception cref="ArgumentNullException">sourceまたはtargetTypeがnullの場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">変換に失敗した場合にスローされます。</exception>
    /// <exception cref="NotSupportedException">変換がサポートされていない場合にスローされます。</exception>
    /// <exception cref="TimeoutException">変換の実行がタイムアウトした場合にスローされます。</exception>
    /// <exception cref="Exception">その他のエラーが発生した場合にスローされます。</exception>
    /// <exception cref="AggregateException">複数の例外が発生した場合にスローされます。</exception>
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