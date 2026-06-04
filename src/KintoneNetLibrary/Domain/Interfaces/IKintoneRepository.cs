using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// Kintoneリポジトリのインターフェース
/// </summary>
public interface IKintoneRepository {
    /// <summary>
    /// レコードを作成する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="records">作成するレコードのリスト</param>
    /// <returns>作成結果を示す文字列</returns>
    Task<string> CreateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを更新する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="records">更新するレコードのリスト</param>
    /// <returns>更新結果を示す文字列</returns>
    Task<string> UpdateRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// レコードを削除する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="records">削除するレコードのリスト</param>
    /// <returns>削除結果を示す文字列</returns>
    Task<string> DeleteRecordsAsync<T>(IList<T> records) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// IDでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="id">検索するレコードのID</param>
    /// <returns>検索結果を示す文字列</returns>
    Task<string?> FindByIDAsync<T>(T model, string id) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// IDsでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="ids">検索するレコードのIDリスト</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>検索結果を示す文字列</returns>
    Task<string?> FindByIDsAsync<T>(T model, IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// 全レコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>検索結果を示す文字列</returns>
    Task<string?> FindAllAsync<T>(T model, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// フィールドでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="field">検索するフィールドの名前</param>
    /// <param name="value">検索する値</param>
    /// <returns>検索結果を示す文字列</returns>
    Task<string?> FindByFieldAsync<T>(T model, string field, string value) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// クエリでレコードを検索する非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="queryStr">検索クエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>検索結果を示す文字列</returns>
    Task<string?> FindByQueryAsync<T>(T model, string queryStr, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// ファイルをアップロードする非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="file">アップロードするファイルの情報を含むFileInfo</param>
    /// <returns>アップロード結果を示す文字列</returns>
    Task<string> UploadFileAsync<T>(T model, FileInfo file) where T : KintoneModelBase<T>, new();
    /// <summary>
    /// ファイルをダウンロードする非同期メソッド
    /// </summary>
    /// <typeparam name="T">Kintoneモデルの型</typeparam>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="fileKey">ダウンロードするファイルのキー</param>
    /// <returns>ダウンロード結果を示すバイト配列</returns>
    Task<byte[]> DownloadFileAsync<T>(T model, string fileKey) where T : KintoneModelBase<T>, new();
}
