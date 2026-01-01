using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Infrastructure.Repositories;

// コメントは日本語で記述
/// <summary>
/// Kintone リポジトリの実装
/// </summary>
public class KintoneRepository(IKintoneApiFactory factory) : IKintoneRepository {
    private readonly IKintoneApiFactory _factory = factory;

    /// <summary>
    /// モデルに対応する KintoneApi を解決する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <returns></returns>
    private KintoneApi ResolveApi<T>(T model) where T : KintoneModelBase<T>, new() {
        return this._factory.Create(model);
    }

    /// <summary>
    /// レコードの作成
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <returns></returns>
    public Task<string> CreateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return this.ExecuteCudAsync(records, (api, json) => api.CreateAsync(json), KintoneRequestBuilder.BuildCreateJson);
    }
    /// <summary>
    /// レコードの更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <returns></returns>
    public Task<string> UpdateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return this.ExecuteCudAsync(records, (api, json) => api.UpdateAsync<T>(json), KintoneRequestBuilder.BuildUpdateJson);
    }
    /// <summary>
    /// レコードの削除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <returns></returns>
    public Task<string> DeleteRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return this.ExecuteCudAsync(records, (api, json) => api.DeleteAsync(json), KintoneRequestBuilder.BuildDeleteJson);
    }
    /// <summary>
    /// ID でレコードを検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<string?> FindByIDAsync<T>(T model, string id) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByIDAsync<T>(id));
    }
    /// <summary>
    /// IDs でレコードを検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="ids"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    public Task<string?> FindByIDsAsync<T>(T model, IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByIDsAsync<T>(ids, fieldCodes));
    }
    /// <summary>
    /// 全レコードを検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    public Task<string?> FindAllAsync<T>(T model, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindAllAsync<T>(fieldCodes));
    }
    /// <summary>
    /// フィールドでレコードを検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Task<string?> FindByFieldAsync<T>(T model, string field, string value) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByFieldAsync<T>(field, value));
    }
    /// <summary>
    /// クエリでレコードを検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="queryStr"></param>
    /// <returns></returns>
    public Task<string?> FindByQueryAsync<T>(T model, string queryStr) where T : KintoneModelBase<T>, new() {
        return this.ExecuteFindAsync(model, api => api.FindByQueryAsync<T>(queryStr));
    }
    /// <summary>
    /// モデルの保存（未実装）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="models"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<KintoneIndexes> SaveAsync<T>(IEnumerable<T> models) where T : KintoneModelBase<T>, new() {
        await Task.CompletedTask.ConfigureAwait(false);
        // 必要であれば保存実装を委譲、それ以外は NotImplemented に
        throw new NotImplementedException("Use KintoneModelCrudService.SaveAsync() instead.");
    }
    /// <summary>
    /// ファイルのアップロード
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public Task<string> UploadFileAsync<T>(T model, FileInfo file) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return api.UploadFileAsync(file);
    }
    /// <summary>
    /// ファイルのダウンロード
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    public Task<byte[]> DownloadFileAsync<T>(T model, string fileKey) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return api.DownloadFileAsync(fileKey);
    }
    /// <summary>
    /// CUD 操作の共通実装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <param name="apiInvoker"></param>
    /// <param name="jsonBuilder"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private async Task<string> ExecuteCudAsync<T>(IList<T> records, Func<KintoneApi, string, Task<string>> apiInvoker, Func<IList<T>, string> jsonBuilder) where T : KintoneModelBase<T>, new() {
        if (records.Count == 0) { throw new ArgumentException("Records list cannot be empty.", nameof(records)); }

        var api = this.ResolveApi(records[0]);
        var json = jsonBuilder(records);
        return await apiInvoker(api, json);
    }
    /// <summary>
    /// 検索操作の共通実装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="apiCall"></param>
    /// <returns></returns>
    private async Task<string?> ExecuteFindAsync<T>(T model, Func<KintoneApi, Task<string>> apiCall) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return await apiCall(api);
    }

}
