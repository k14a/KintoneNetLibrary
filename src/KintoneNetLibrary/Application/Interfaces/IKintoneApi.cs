using KintoneNetLibrary.Domain.Entities;
using System.Text.Json;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintone API - レコード取得インターフェイス
/// </summary>
public interface IKintoneApi {
    /// <summary>
    /// IDで単一レコードを取得
    /// </summary>
    /// <typeparam name="T">取得するレコードの型</typeparam>
    /// <param name="id">レコードのID</param>
    /// <returns>取得されたレコードのJSON文字列</returns>
    Task<string?> FindByIDAsync<T>(string id) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// IDで単一レコードを取得（Raw）
    /// </summary>
    /// <param name="id">レコードのID</param>
    /// <returns>取得されたレコードのJSON文字列（Raw）</returns>
    Task<string?> RawFindByIDAsync(string id);

    /// <summary>
    /// IDで単一レコードを取得（Raw・ストリーム）
    /// </summary>
    /// <param name="output">出力先のストリーム</param>
    /// <param name="id">レコードのID</param>
    /// <returns></returns>
    Task RawFindByIDAsStreamAsync(Stream output, string id);

    /// <summary>
    /// IDリストで複数レコードを取得
    /// </summary>
    /// <typeparam name="T">取得するレコードの型</typeparam>
    /// <param name="ids">レコードのIDリスト</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>取得されたレコードのJSON文字列</returns>
    Task<string?> FindByIDsAsync<T>(IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// IDリストで複数レコードを取得（Raw）
    /// </summary>
    /// <param name="ids">レコードのIDリスト</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>取得されたレコードのJSON文字列（Raw）</returns>
    Task<string?> RawFindByIDsAsync(IList<string> ids, IList<string>? fieldCodes = null);

    /// <summary>
    /// IDリストで複数レコードを取得（Raw・ストリーム）
    /// </summary>
    /// <param name="output">出力先のストリーム</param>
    /// <param name="ids">レコードのIDリスト</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns></returns>
    Task RawFindByIDsAsStreamAsync(Stream output, IList<string> ids, IList<string>? fieldCodes = null);

    /// <summary>
    /// 全レコード取得（条件なし）
    /// </summary>
    /// <typeparam name="T">取得するレコードの型</typeparam>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>取得されたレコードのJSON文字列</returns>
    Task<string?> FindAllAsync<T>(IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 全レコード取得（条件なし・Raw）
    /// </summary>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>取得されたレコードのJSON文字列（Raw）</returns>
    Task<string?> RawFindAllAsync(IList<string>? fieldCodes = null);

    /// <summary>
    /// 全レコード取得（条件なし・Raw・ストリーム）
    /// </summary>
    /// <param name="output">出力先のストリーム</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns></returns>
    Task RawFindAllAsStreamAsync(Stream output, IList<string>? fieldCodes = null);

    /// <summary>
    /// 指定フィールド＝値 で検索
    /// </summary>
    /// <typeparam name="T">取得するレコードの型</typeparam>
    /// <param name="field">検索するフィールドコード</param>
    /// <param name="value">検索する値</param>
    /// <returns>取得されたレコードのJSON文字列</returns>
    Task<string?> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw）
    /// </summary>
    /// <param name="field">検索するフィールドコード</param>
    /// <param name="value">検索する値</param>
    /// <returns>取得されたレコードのJSON文字列（Raw）</returns>
    Task<string?> RawFindByFieldAsync(string field, string value);

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw・ストリーム）
    /// </summary>
    /// <param name="output">出力先のストリーム</param>
    /// <param name="field">検索するフィールドコード</param>
    /// <param name="value">検索する値</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns></returns>
    Task RawFindByFieldAsStreamAsync(Stream output, string field, string value, IList<string>? fieldCodes = null);

    /// <summary>
    /// クエリ文字列で検索
    /// </summary>
    /// <typeparam name="T">取得するレコードの型</typeparam>
    /// <param name="queryStr">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>取得されたレコードのJSON文字列</returns>
    Task<string?> FindByQueryAsync<T>(string queryStr, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// クエリ文字列で検索（Raw）
    /// </summary>
    /// <param name="queryStr">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>取得されたレコードのJSON文字列（Raw）</returns>
    Task<string?> RawFindByQueryAsync(string queryStr, IList<string>? fieldCodes = null);

    /// <summary>
    /// クエリ文字列で検索（Raw・ストリーム）
    /// </summary>
    /// <param name="output">出力先のストリーム</param>
    /// <param name="queryStr">検索するクエリ文字列</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns></returns>
    Task RawFindByQueryAsStreamAsync(Stream output, string queryStr, IList<string>? fieldCodes = null);

    /// <summary>
    /// 複数レコードを一括登録します
    /// </summary>
    /// <param name="json">登録するレコードのJSON文字列</param>
    /// <returns>登録結果のJSON文字列</returns>
    Task<string> CreateAsync(string json);

    /// <summary>
    /// 複数レコードを一括登録します（Raw）
    /// </summary>
    /// <param name="json">登録するレコードのJSON文字列（Raw）</param>
    /// <returns>登録結果のJSON文字列（Raw）</returns>
    Task<string> RawCreateAsync(string json);

    /// <summary>
    /// 複数レコードを一括更新します
    /// </summary>
    /// <typeparam name="T">更新するレコードの型</typeparam>
    /// <param name="json">更新するレコードのJSON文字列</param>
    /// <returns>更新結果のJSON文字列</returns>
    Task<string> UpdateAsync<T>(string json) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 複数レコードを一括更新します（Raw）
    /// </summary>
    /// <param name="json">更新するレコードのJSON文字列（Raw）</param>
    /// <returns>更新結果のJSON文字列（Raw）</returns>
    Task<string> RawUpdateAsync(string json);

    /// <summary>
    /// 複数レコードを一括削除します
    /// </summary>
    /// <param name="json">削除するレコードのJSON文字列</param>
    /// <returns>削除結果のJSON文字列</returns>
    Task<string> DeleteAsync(string json);

    /// <summary>
    /// 複数レコードを一括削除します（Raw）
    /// </summary>
    /// <param name="json">削除するレコードのJSON文字列（Raw）</param>
    /// <returns>削除結果のJSON文字列（Raw）</returns>
    Task<string> RawDeleteAsync(string json);

    /// <summary>
    /// ファイルをアップロードします
    /// </summary>
    /// <param name="stream">アップロードするファイルのストリーム</param>
    /// <param name="fileName">アップロードするファイルの名前</param>
    /// <returns>アップロード結果のJSON文字列</returns>
    Task<string> UploadFileAsync(Stream stream, string fileName);

    /// <summary>
    /// ファイルをアップロードします（キャンセルトークン付き）
    /// </summary>
    /// <param name="stream">アップロードするファイルのストリーム</param>
    /// <param name="fileName">アップロードするファイルの名前</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>アップロード結果のJSON文字列</returns>
    Task<string> UploadFileAsync(Stream stream, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// ファイルをアップロードします
    /// </summary>
    /// <param name="file">アップロードするファイルの情報</param>
    /// <returns>アップロード結果のJSON文字列</returns>
    Task<string> UploadFileAsync(FileInfo file);

    /// <summary>
    /// ファイルをダウンロードします
    /// </summary>
    /// <param name="fileKey">ダウンロードするファイルのキー</param>
    /// <returns>ダウンロード結果のバイト配列</returns>
    Task<byte[]> DownloadFileAsync(string fileKey);

    /// <summary>
    /// ファイルをストリームでダウンロードします
    /// </summary>
    /// <param name="fileKey">ダウンロードするファイルのキー</param>
    /// <returns>ダウンロード結果のストリーム</returns>
    Task<Stream> DownloadFileStreamAsync(string fileKey);

    /// <summary>
    /// ファイルを指定先にダウンロードします
    /// </summary>
    /// <param name="fileKey">ダウンロードするファイルのキー</param>
    /// <param name="destination">ダウンロード先のファイル情報</param>
    /// <returns></returns>
    Task DownloadFileAsync(string fileKey, FileInfo destination);

    /// <summary>
    /// カーソルを作成します
    /// </summary>
    /// <param name="body">カーソル作成に必要な情報を含む辞書</param>
    /// <returns>作成されたカーソルのID</returns>
    Task<string> CreateCursorAsync(Dictionary<string, object> body);

    /// <summary>
    /// カーソルを作成します
    /// </summary>
    /// <param name="query">検索するクエリ文字列</param>
    /// <param name="fields">取得するフィールドコードのリスト（オプション）</param>
    /// <param name="size">カーソルページサイズ（オプション）</param>
    /// <returns>作成されたカーソルのID</returns>
    Task<string> CreateCursorAsync(string query, IList<string>? fields = null, int? size = null);

    /// <summary>
    /// カーソルをストリームで取得します。
    /// </summary>
    /// <param name="cursorId">取得するカーソルのID</param>
    /// <returns>カーソルページのストリーム</returns>
    Task<Stream> FetchCursorPageAsStreamAsync(string cursorId);

    /// <summary>
    /// カーソルを削除します。
    /// </summary>
    /// <param name="json">削除するカーソルの情報を含むJSON文字列</param>
    /// <returns>削除結果のJSON文字列</returns>
    Task<string> DeleteCursorJsonAsync(string json);

    /// <summary>
    /// カーソルを削除します。
    /// </summary>
    /// <param name="cursorId">削除するカーソルのID</param>
    /// <returns>削除結果のJSON文字列</returns>
    Task DeleteCursorAsync(string cursorId);

    /// <summary>
    /// カーソルをストリームで取得します
    /// </summary>
    /// <param name="cursorId">取得するカーソルのID</param>
    /// <returns>カーソルページのストリーム</returns>
    IAsyncEnumerable<Stream> StreamCursorAsync(string cursorId);

    /// <summary>
    /// カーソルをストリームで取得します
    /// </summary>
    /// <param name="cursorId">取得するカーソルのID</param>
    /// <returns>カーソルページのストリーム</returns>
    IAsyncEnumerable<Stream> StreamCursorStreamAsync(string cursorId);

    /// <summary>
    /// カーソルページをストリームで取得します
    /// </summary>
    /// <param name="query">検索するクエリ文字列</param>
    /// <param name="fields">取得するフィールドコードのリスト（オプション）</param>
    /// <param name="size">カーソルページサイズ（オプション）</param>
    /// <returns>カーソルページのストリーム</returns>
    IAsyncEnumerable<Stream> StreamCursorPagesAsync(string query, IList<string>? fields = null, int? size = null);

    /// <summary>
    /// カーソルページをストリームで取得します
    /// </summary>
    /// <param name="query">検索するクエリ文字列</param>
    /// <param name="fields">取得するフィールドコードのリスト（オプション）</param>
    /// <param name="size">カーソルページサイズ（オプション）</param>
    /// <returns>カーソルページのJSON要素</returns>
    IAsyncEnumerable<JsonElement> StreamRecordsAsync(string query, IList<string>? fields = null, int? size = null);

    /// <summary>
    /// カーソルページサイズ
    /// </summary>
    int CursorPageSize { get; set; }
}