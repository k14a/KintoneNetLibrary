using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Application.UseCases;

namespace KintoneNetLibrary.Domain.Interfaces;

// コメントは日本語で記述
/// <summary>
/// Kintoneリポジトリのインターフェース
/// </summary>
public interface IKintoneRepository {
    /// <summary>
    /// レコードを作成する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <returns></returns>
    Task<string> CreateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを更新する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <returns></returns>
    Task<string> UpdateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを削除する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="records"></param>
    /// <returns></returns>
    Task<string> DeleteRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// IDでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<string?> FindByIDAsync<T>(T model, string id) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// IDsでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="ids"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> FindByIDsAsync<T>(T model, IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// 全レコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> FindAllAsync<T>(T model, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// フィールドでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<string?> FindByFieldAsync<T>(T model, string field, string value) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// クエリでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="queryStr"></param>
    /// <returns></returns>
    Task<string?> FindByQueryAsync<T>(T model, string queryStr) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// ファイルをアップロードする非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<string> UploadFileAsync<T>(T model, FileInfo file) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// ファイルをダウンロードする非同期メソッド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    Task<byte[]> DownloadFileAsync<T>(T model, string fileKey) where T : KintoneModelBase<T>, new();
}
