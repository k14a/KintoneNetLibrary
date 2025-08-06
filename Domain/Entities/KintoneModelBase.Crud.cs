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

    public static async Task<KintoneWriteResult<TSelf>> CreateBulkAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.CreateAsync([.. models], enableSingleRetryOnError);
    }
    public static async Task<KintoneWriteResult<TSelf>> CreateSingleAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.CreateAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }
    public static async Task<KintoneWriteResult<TSelf>> UpdateBulkAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.UpdateAsync([.. models], enableSingleRetryOnError);
    }
    public static async Task<KintoneWriteResult<TSelf>> UpdateSingleAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.UpdateAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }
    public static async Task<KintoneDeleteResult> DeleteBulkAsync(IEnumerable<TSelf> models, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.DeleteAsync([.. models], validateExistence);
    }
    public static async Task<KintoneDeleteResult> DeleteSingleAsync(IEnumerable<TSelf> models, bool validateExistence = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneDeleteResult();

        foreach (var model in models) {
            var result = await service.DeleteAsync([model], validateExistence);
            mergedResult.DeletedIDs.AddRange(result.DeletedIDs);
            mergedResult.FailedIDs.AddRange(result.FailedIDs);
        }

        return mergedResult;
    }
    public static async Task<KintoneWriteResult<TSelf>> SaveBulkAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.SaveAsync([.. models], enableSingleRetryOnError);
    }

    public static async Task<KintoneWriteResult<TSelf>> SaveSingleAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        var mergedResult = new KintoneWriteResult<TSelf>();

        foreach (var model in models) {
            var result = await service.SaveAsync([model], enableSingleRetryOnError);
            mergedResult.Merge(result);
        }

        return mergedResult;
    }
    public static async Task<KintoneWriteResult<TSelf>> SaveWithRetryBulkAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return await service.SaveWithRetryAsync([.. models], enableSingleRetryOnError, enableCreateToUpdateRetry);
    }

    public static async Task<KintoneWriteResult<TSelf>> SaveWithRetrySingleAsync(IEnumerable<TSelf> models, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
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

    public static async Task<List<TSelf>> FindByIDsAsync(IEnumerable<string> ids) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return (await service.FindAsync<TSelf>(ids.ToList(), fieldCodes: null)).ToList();
    }
    public static async Task<TSelf?> FindByKeyAsync(TSelf model) {
        KintoneModelValidator.ValidateKeyIntegrity(model);

        var keyProp = typeof(TSelf)
            .GetProperties()
            .First(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        var keyValue = keyProp.GetValue(model);
        if (keyValue == null) { return null; }

        var query = new KintoneQuery<TSelf>().Equal(CreateFieldSelector(keyProp), keyValue);
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return (await service.FindAsync<TSelf>(query: query.Build())).FirstOrDefault();
    }

    public static async Task<List<TSelf>> FindByKeysAsync(IEnumerable<TSelf> models) {
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

        var query = new KintoneQuery<TSelf>().In(CreateFieldSelector(keyProp), keyValues);
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return (await service.FindAsync<TSelf>(query: query.Build())).ToList();
    }


    public static async Task<List<TSelf>> FindByQueryAsync(string query) {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return (await service.FindAsync<TSelf>(query: query)).ToList();
    }

    public static async Task<List<TSelf>> FindAllAsync() {
        var service = KintoneServiceLocator.Resolve<IKintoneModelCrudService>();
        return (await service.FindAsync<TSelf>()).ToList();
    }

    private static Expression<Func<TSelf, object>> CreateFieldSelector(PropertyInfo prop) {
        var param = Expression.Parameter(typeof(TSelf), "x");
        var access = Expression.Convert(Expression.Property(param, prop), typeof(object));
        return Expression.Lambda<Func<TSelf, object>>(access, param);
    }

}