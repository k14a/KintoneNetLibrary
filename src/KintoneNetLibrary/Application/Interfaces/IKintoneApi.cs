using KintoneNetLibrary.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintone API - レコード取得インターフェイス
/// </summary>
public interface IKintoneApi {
    /// <summary>
    /// IDで単一レコードを取得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<string?> FindByIDAsync<T>(string id) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// IDで単一レコードを取得（Raw）
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<string?> RawFindByIDAsync(string id);

    /// <summary>
    /// IDリストで複数レコードを取得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ids"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> FindByIDsAsync<T>(IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// IDリストで複数レコードを取得（Raw）
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> RawFindByIDsAsync(IList<string> ids, IList<string>? fieldCodes = null);

    /// <summary>
    /// 全レコード取得（条件なし）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> FindAllAsync<T>(IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 全レコード取得（条件なし・Raw）
    /// </summary>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> RawFindAllAsync(IList<string>? fieldCodes = null);

    /// <summary>
    /// 指定フィールド＝値 で検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<string?> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw）
    /// </summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<string?> RawFindByFieldAsync(string field, string value);

    /// <summary>
    /// クエリ文字列で検索
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryStr"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> FindByQueryAsync<T>(string queryStr, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// クエリ文字列で検索（Raw）
    /// </summary>
    /// <param name="queryStr"></param>
    /// <param name="fieldCodes"></param>
    /// <returns></returns>
    Task<string?> RawFindByQueryAsync(string queryStr, IList<string>? fieldCodes = null);

    /// <summary>
    /// 複数レコードを一括登録します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    Task<string> CreateAsync(string json);

    /// <summary>
    /// 複数レコードを一括登録します（Raw）
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    Task<string> RawCreateAsync(string json);

    /// <summary>
    /// 複数レコードを一括更新します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    Task<string> UpdateAsync<T>(string json) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 複数レコードを一括更新します（Raw）
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    Task<string> RawUpdateAsync(string json);

    /// <summary>
    /// 複数レコードを一括削除します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    Task<string> DeleteAsync(string json);

    /// <summary>
    /// 複数レコードを一括削除します（Raw）
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    Task<string> RawDeleteAsync(string json);

    /// <summary>
    /// ファイルをアップロードします
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task<string> UploadFileAsync(Stream stream, string fileName);

    /// <summary>
    /// ファイルをアップロードします（キャンセルトークン付き）
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="fileName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string> UploadFileAsync(Stream stream, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// ファイルをアップロードします
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<string> UploadFileAsync(FileInfo file);

    /// <summary>
    /// ファイルをダウンロードします
    /// </summary>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    Task<byte[]> DownloadFileAsync(string fileKey);

    /// <summary>
    /// ファイルをストリームでダウンロードします
    /// </summary>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    Task<Stream> DownloadFileStreamAsync(string fileKey);

    /// <summary>
    /// ファイルを指定先にダウンロードします
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    Task DownloadFileAsync(string fileKey, FileInfo destination);

    /// <summary>
    /// カーソルを作成します
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    Task<string> CreateCursorAsync(Dictionary<string, object> body);

    /// <summary>
    /// カーソルを取得します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    Task<string> DeleteCursorJsonAsync(string json);

    /// <summary>
    /// カーソルをストリームで取得します
    /// </summary>
    /// <param name="cursorId"></param>
    /// <returns></returns>
    IAsyncEnumerable<string> StreamCursorAsync(string cursorId);

    /// <summary>
    /// カーソルページサイズ
    /// </summary>
    int CursorPageSize { get; set; }
}