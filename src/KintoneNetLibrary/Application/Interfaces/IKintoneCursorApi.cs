using System.Text.Json;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintone カーソル API インターフェイス
/// </summary>
public interface IKintoneCursorApi {
    /// <summary>
    /// カーソルページサイズ
    /// </summary>
    int CursorPageSize { get; set; }

    /// <summary>
    /// カーソルを作成します
    /// </summary>
    Task<string> CreateCursorAsync(Dictionary<string, object> body);

    /// <summary>
    /// カーソルを作成します
    /// </summary>
    Task<string> CreateCursorAsync(string query, IList<string>? fields = null, int? size = null);

    /// <summary>
    /// カーソルをストリームで取得します
    /// </summary>
    Task<Stream> FetchCursorPageAsStreamAsync(string cursorId);

    /// <summary>
    /// カーソルを削除します
    /// </summary>
    Task<string> DeleteCursorJsonAsync(string json);

    /// <summary>
    /// カーソルを削除します
    /// </summary>
    Task DeleteCursorAsync(string cursorId);

    /// <summary>
    /// カーソルをストリームで取得します
    /// </summary>
    IAsyncEnumerable<Stream> StreamCursorAsync(string cursorId);

    /// <summary>
    /// カーソルをストリームで取得します
    /// </summary>
    IAsyncEnumerable<Stream> StreamCursorStreamAsync(string cursorId);

    /// <summary>
    /// カーソルページをストリームで取得します
    /// </summary>
    IAsyncEnumerable<Stream> StreamCursorPagesAsync(string query, IList<string>? fields = null, int? size = null);

    /// <summary>
    /// カーソルページをJSON要素のストリームで取得します
    /// </summary>
    IAsyncEnumerable<JsonElement> StreamRecordsAsync(string query, IList<string>? fields = null, int? size = null);
}
