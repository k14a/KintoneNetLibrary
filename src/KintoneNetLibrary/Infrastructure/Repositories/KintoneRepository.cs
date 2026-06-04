using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Infrastructure.Repositories;

/// <summary>
/// Kintone API を利用してレコードの CRUD 操作を行うリポジトリクラス
/// </summary>
/// <param name="factory">Kintone API ファクトリ</param>
public class KintoneRepository(IKintoneApiFactory factory) : IKintoneRepository {
    private readonly IKintoneApiFactory _factory = factory;

    /// <summary>
    /// モデルに対応する KintoneApi を解決する
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <returns>対応する KintoneApi のインスタンス</returns>
    private IKintoneApi ResolveApi<T>(T model) where T : KintoneModelBase<T>, new() {
        return this._factory.Create(model);
    }

    /// <summary>
    /// レコードの作成
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">作成対象のレコードリスト</param>
    /// <returns>作成結果の JSON 文字列</returns>
    public async Task<string> CreateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteCudAsync(records, (api, json) => api.CreateAsync(json), KintoneRequestBuilder.BuildCreateJson);
    }

    /// <summary>
    /// レコードの更新
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">更新対象のレコードリスト</param>
    /// <returns>更新結果の JSON 文字列</returns>
    public async Task<string> UpdateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteCudAsync(records, (api, json) => api.UpdateAsync<T>(json), KintoneRequestBuilder.BuildUpdateJson);
    }

    /// <summary>
    /// レコードの削除
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">削除対象のレコードリスト</param>
    /// <returns>削除結果の JSON 文字列</returns>
    public async Task<string> DeleteRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteCudAsync(records, (api, json) => api.DeleteAsync(json), KintoneRequestBuilder.BuildDeleteJson);
    }

    /// <summary>
    /// ID でレコードを検索
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="id">検索対象のレコードID</param>
    /// <returns>検索結果の JSON 文字列</returns>
    public async Task<string?> FindByIDAsync<T>(T model, string id) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteFindAsync(model, api => api.FindByIDAsync<T>(id));
    }

    /// <summary>
    /// IDs でレコードを検索
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="ids">検索対象のレコードIDリスト</param>
    /// <param name="fieldCodes">取得対象のフィールドコードリスト</param>
    /// <returns>検索結果の JSON 文字列</returns>
    public async Task<string?> FindByIDsAsync<T>(T model, IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteFindAsync(model, api => api.FindByIDsAsync<T>(ids, fieldCodes));
    }

    /// <summary>
    /// 全レコードを検索
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="fieldCodes">取得対象のフィールドコードリスト</param>
    /// <returns>検索結果の JSON 文字列</returns>
    public async Task<string?> FindAllAsync<T>(T model, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteFindAsync(model, api => api.FindAllAsync<T>(fieldCodes));
    }

    /// <summary>
    /// フィールドでレコードを検索
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="field">検索対象のフィールドコード</param>
    /// <param name="value">検索対象の値</param>
    /// <returns>検索結果の JSON 文字列</returns>
    public async Task<string?> FindByFieldAsync<T>(T model, string field, string value) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteFindAsync(model, api => api.FindByFieldAsync<T>(field, value));
    }

    /// <summary>
    /// クエリでレコードを検索
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="queryStr">検索対象のクエリ文字列</param>
    /// <returns>検索結果の JSON 文字列</returns>
    public async Task<string?> FindByQueryAsync<T>(T model, string queryStr, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        return await this.ExecuteFindAsync(model, api => api.FindByQueryAsync<T>(queryStr, fieldCodes));
    }

    /// <summary>
    /// モデルの保存（未実装）
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="models">保存対象のモデルリスト</param>
    /// <returns>保存結果のインデックス情報</returns>
    /// <exception cref="NotImplementedException">このメソッドは未実装です。KintoneModelCrudService.SaveAsync() を使用してください。</exception>
    public async Task<KintoneIndexes> SaveAsync<T>(IEnumerable<T> models) where T : KintoneModelBase<T>, new() {
        await Task.CompletedTask.ConfigureAwait(false);
        // 必要であれば保存実装を委譲、それ以外は NotImplemented に
        throw new NotImplementedException("Use KintoneModelCrudService.SaveAsync() instead.");
    }

    /// <summary>
    /// ファイルのアップロード
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="file">アップロード対象のファイル情報</param>
    /// <returns>アップロード結果のファイルキー</returns>
    public Task<string> UploadFileAsync<T>(T model, FileInfo file) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return api.UploadFileAsync(file);
    }

    /// <summary>
    /// ファイルのダウンロード
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="fileKey">ダウンロード対象のファイルキー</param>
    /// <returns>ダウンロード結果のバイト配列</returns>
    public Task<byte[]> DownloadFileAsync<T>(T model, string fileKey) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return api.DownloadFileAsync(fileKey);
    }

    /// <summary>
    /// CUD 操作の共通実装
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="records">操作対象のモデルリスト</param>
    /// <param name="apiInvoker">API 呼び出しのデリゲート</param>
    /// <param name="jsonBuilder">JSON 文字列生成のデリゲート</param>
    /// <returns>操作結果の JSON 文字列</returns>
    /// <exception cref="ArgumentException">レコードリストが空の場合にスローされます</exception>
    private async Task<string> ExecuteCudAsync<T>(IList<T> records, Func<IKintoneApi, string, Task<string>> apiInvoker, Func<IList<T>, string> jsonBuilder) where T : KintoneModelBase<T>, new() {
        if (records.Count == 0) { throw new ArgumentException("Records list cannot be empty.", nameof(records)); }

        var api = this.ResolveApi(records[0]);
        var json = jsonBuilder(records);
        return await apiInvoker(api, json);
    }

    /// <summary>
    /// 検索操作の共通実装
    /// </summary>
    /// <typeparam name="T">モデルの型</typeparam>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="apiCall">API 呼び出しのデリゲート</param>
    /// <returns>検索結果の JSON 文字列</returns>
    private async Task<string?> ExecuteFindAsync<T>(T model, Func<IKintoneApi, Task<string?>> apiCall) where T : KintoneModelBase<T>, new() {
        var api = this.ResolveApi(model);
        return await apiCall(api);
    }
}
