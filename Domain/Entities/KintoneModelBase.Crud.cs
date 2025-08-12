using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    protected IKintoneModelCrudService _service => KintoneServiceLocator.Resolve<IKintoneModelCrudService>();

    public async Task<KintoneWriteResult<TSelf>> CreateAsync(bool enableSingleRetryOnError = false) {
        return await this._service.CreateAsync([(TSelf)this], enableSingleRetryOnError);
    }
    public async Task<KintoneWriteResult<TSelf>> UpdateAsync(bool enableSingleRetryOnError = false) {
        return await this._service.UpdateAsync([(TSelf)this], enableSingleRetryOnError);
    }
    public async Task<KintoneDeleteResult> DeleteAsync(bool validateExistence = true) {
        return await this._service.DeleteAsync([(TSelf)this], validateExistence);
    }
    public async Task<KintoneWriteResult<TSelf>> SaveAsync(bool enableSingleRetryOnError = false) {
        return await this._service.SaveAsync([(TSelf)this], enableSingleRetryOnError);
    }
    public async Task<KintoneWriteResult<TSelf>> SaveWithRetryAsync(bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        return await this._service.SaveWithRetryAsync([(TSelf)this], enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

    public static async Task<KintoneWriteResult<TSelf>> CreateBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.CreateAsync(models, enableSingleRetryOnError);
    }
    public static async Task<KintoneWriteResult<TSelf>> CreateSingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.CreateAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }
    public static async Task<KintoneWriteResult<TSelf>> UpdateBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.UpdateAsync(models, enableSingleRetryOnError);
    }
    public static async Task<KintoneWriteResult<TSelf>> UpdateSingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.UpdateAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }
    public static async Task<KintoneDeleteResult> DeleteBulkAsync(IList<string> ids, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.DeleteAsync<TSelf>(ids, validateExistence);
    }
    public static async Task<KintoneDeleteResult> DeleteBulkAsync(IList<TSelf> models, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.DeleteAsync(models, validateExistence);
    }
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
    public static async Task<KintoneWriteResult<TSelf>> SaveBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.SaveAsync(models, enableSingleRetryOnError);
    }
    public static async Task<KintoneWriteResult<TSelf>> SaveSingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.SaveAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }
    public static async Task<KintoneWriteResult<TSelf>> SaveWithRetryBulkAsync(IList<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.SaveWithRetryAsync(models, enableSingleRetryOnError, enableCreateToUpdateRetry);
    }
    public static async Task<KintoneWriteResult<TSelf>> SaveWithRetrySingleAsync(IList<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.SaveWithRetryAsync([model], enableSingleRetryOnError, enableCreateToUpdateRetry);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }
    public static async Task<TSelf?> FindByIDAsync(string id) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var result = await service.FindAsync<TSelf>([id], fieldCodes: null);
        return result.FirstOrDefault();
    }
    public static async Task<List<TSelf>> FindByIDsAsync(IList<string> ids) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return [.. await service.FindAsync<TSelf>([.. ids], fieldCodes: null)];
    }
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
    public static async Task<List<TSelf>> FindByQueryAsync(string query) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return [.. await service.FindAsync<TSelf>(query: query)];
    }
    public static async Task<List<TSelf>> FindAllAsync() {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return [.. await service.FindAsync<TSelf>()];
    }

    private static Expression<Func<TSelf, TValue>> CreateFieldSelector<TValue>(PropertyInfo prop) {
        var param = Expression.Parameter(typeof(TSelf), "x");
        var body = Expression.Property(param, prop.Name);
        return Expression.Lambda<Func<TSelf, TValue>>(body, param);
    }
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