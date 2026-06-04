namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintone ファイル操作 API インターフェイス
/// </summary>
public interface IKintoneFileApi {
    /// <summary>
    /// ファイルをアップロードします
    /// </summary>
    Task<string> UploadFileAsync(Stream stream, string fileName);

    /// <summary>
    /// ファイルをアップロードします（キャンセルトークン付き）
    /// </summary>
    Task<string> UploadFileAsync(Stream stream, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// ファイルをアップロードします
    /// </summary>
    Task<string> UploadFileAsync(FileInfo file);

    /// <summary>
    /// ファイルをダウンロードします
    /// </summary>
    Task<byte[]> DownloadFileAsync(string fileKey);

    /// <summary>
    /// ファイルをストリームでダウンロードします
    /// </summary>
    Task<Stream> DownloadFileStreamAsync(string fileKey);

    /// <summary>
    /// ファイルを指定先にダウンロードします
    /// </summary>
    Task DownloadFileAsync(string fileKey, FileInfo destination);
}
