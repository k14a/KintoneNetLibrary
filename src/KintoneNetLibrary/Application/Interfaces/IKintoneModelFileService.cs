using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintoneモデルのファイル操作を提供するサービスインターフェイスです。
/// </summary>
/// <typeparam name="T">Kintoneモデルの型</typeparam>
public interface IKintoneModelFileService<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// ファイルをアップロードします。
    /// </summary>
    /// <param name="filePath">アップロードするファイルのパス</param>
    /// <returns>アップロードされたファイルのキー</returns>
    Task<string> UploadFileAsync(string filePath);
    /// <summary>
    /// ファイルをアップロードします。
    /// </summary>
    /// <param name="model">アップロード対象のモデル</param>
    /// <param name="file">アップロードするファイル情報</param>
    /// <returns>アップロードされたファイル情報</returns>
    Task<KintoneFile> UploadFileAsync(T model, FileInfo file);
    /// <summary>
    /// 複数ファイルをアップロードします。
    /// </summary>
    /// <param name="model">アップロード対象のモデル</param>
    /// <returns>アップロードされたファイル情報のリスト</returns>
    Task<List<KintoneFile>> UploadFilesAsync(T model);
    /// <summary>
    /// 複数ファイルをアップロードします。
    /// </summary>
    /// <param name="model">アップロード対象のモデル</param>
    /// <param name="files">アップロードするファイル情報のリスト</param>
    /// <returns>アップロードされたファイル情報のリスト</returns>
    Task<IEnumerable<KintoneFile>> UploadFilesAsync(T model, IEnumerable<FileInfo> files);
    /// <summary>
    /// アップロードされたファイルをモデルにマッピングします。
    /// </summary>
    /// <param name="model">マッピング対象のモデル</param>
    /// <param name="files">マッピングするファイル情報のリスト</param>
    /// <returns></returns>
    Task MapUploadedFilesToModelAsync(T model, IEnumerable<FileInfo> files);

    /// <summary>
    /// ファイルをダウンロードします。
    /// </summary>
    /// <param name="fileKey">ダウンロードするファイルのキー</param>
    /// <returns>ダウンロードされたファイルのバイト配列</returns>
    Task<byte[]> DownloadFileAsync(string fileKey);
    /// <summary>
    /// ファイルをダウンロードします。
    /// </summary>
    /// <param name="model">ダウンロード対象のモデル</param>
    /// <param name="files">ダウンロードするファイル情報のリスト</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ（オプション）</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか（デフォルト: true）</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか（デフォルト: false）</param>
    /// <returns>ダウンロードされたファイル情報のリスト</returns>
    Task<IEnumerable<FileInfo>> DownloadFilesAsync(T model, IEnumerable<KintoneFile> files, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// ファイルをダウンロードします。
    /// </summary>
    /// <param name="model">ダウンロード対象のモデル</param>
    /// <param name="file">ダウンロードするファイル情報</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ（オプション）</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか（デフォルト: true）</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか（デフォルト: false）</param>
    /// <returns>ダウンロードされたファイル情報</returns>
    Task<FileInfo> DownloadFileAsync(T model, KintoneFile file, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// ファイルを指定パスにダウンロードします。
    /// </summary>
    /// <param name="model">ダウンロード対象のモデル</param>
    /// <param name="file">ダウンロードするファイル情報</param>
    /// <param name="savePath">保存先のパス</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか（デフォルト: true）</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか（デフォルト: false）</param>
    /// <returns>ダウンロードされたファイル情報</returns>
    Task<FileInfo> DownloadFileToPathAsync(T model, KintoneFile file, string savePath, bool overwrite = true, bool throwIfExists = false);

}