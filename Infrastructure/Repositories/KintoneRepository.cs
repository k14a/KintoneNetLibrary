using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Infrastructure.Repositories;

public class KintoneRepository : IKintoneRepository {
    private readonly IKintoneApiFactory _factory;

    public KintoneRepository(IKintoneApiFactory factory) {
        this._factory = factory;
    }

    private KintoneApi ResolveApi<T>(T model) where T : KintoneModelBase<T>, new() {
        return this._factory.Create(model);
    }

    public Task<string> CreateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return this.ExecuteCudAsync(records, (api, json) => api.CreateAsync(json), KintoneRequestBuilder.BuildCreateJson);
    }
    public Task<string> UpdateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return this.ExecuteCudAsync(records, (api, json) => api.UpdateAsync<T>(json), KintoneRequestBuilder.BuildUpdateJson);
    }
    public Task<string> DeleteRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return this.ExecuteCudAsync(records, (api, json) => api.DeleteAsync(json), KintoneRequestBuilder.BuildDeleteJson);
    }
    public Task<string?> FindByIDAsync<T>(T model, string id) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByIDAsync<T>(id));
    }

    public Task<string?> FindByIDsAsync<T>(T model, IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByIDsAsync<T>(ids, fieldCodes));
    }

    public Task<string?> FindAllAsync<T>(T model, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindAllAsync<T>(fieldCodes));
    }

    public Task<string?> FindByFieldAsync<T>(T model, string field, string value) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByFieldAsync<T>(field, value));
    }

    public Task<string?> FindByQueryAsync<T>(T model, string queryStr) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByQueryAsync<T>(queryStr));
    }

    public async Task<KintoneIndexes> SaveAsync<T>(IEnumerable<T> models) where T : KintoneModelBase<T>, new() {
        await Task.CompletedTask.ConfigureAwait(false);
        // 必要であれば保存実装を委譲、それ以外は NotImplemented に
        throw new NotImplementedException("Use KintoneModelCrudService.SaveAsync() instead.");
    }
    public Task<string> UploadFileAsync<T>(T model, FileInfo file) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return api.UploadFileAsync(file);
    }

    public Task<byte[]> DownloadFileAsync<T>(T model, string fileKey) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return api.DownloadFileAsync(fileKey);
    }

    private async Task<string> ExecuteCudAsync<T>(IList<T> records, Func<KintoneApi, string, Task<string>> apiInvoker, Func<IList<T>, string> jsonBuilder) where T : KintoneModelBase<T>, new() {
        if (records.Count == 0) { throw new ArgumentException("Records list cannot be empty.", nameof(records)); }

        var api = this.ResolveApi(records[0]);
        var json = jsonBuilder(records);
        return await apiInvoker(api, json);
    }
    private async Task<string?> ExecuteFindAsync<T>(T model, Func<KintoneApi, Task<string>> apiCall) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return await apiCall(api);
    }

}
