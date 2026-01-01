using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

// コメントは日本語で記述
/// <summary>
/// Kintoneのファイル操作サービスのインターフェース
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKintoneModelFileService<T> where T : KintoneModelBase<T>, new() {
    #region <<Upload methods>>
    /// <summary>
    /// ファイルをKintoneにアップロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<KintoneFile> UploadFileAsync(T model);
    /// <summary>
    /// ファイルをKintoneにアップロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<KintoneFile> UploadFileAsync(T model, FileInfo file);
    /// <summary>
    /// 複数のファイルをKintoneにアップロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    Task<IEnumerable<KintoneFile>> UploadFilesAsync(T model, IEnumerable<FileInfo> files);
    /// <summary>
    /// アップロードしたファイルをモデルにマッピングします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    Task MapUploadedFilesToModelAsync(T model, IEnumerable<FileInfo> files);
    #endregion

    #region <<Download methods>>
    /// <summary>
    /// Kintoneからファイルをダウンロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="overwrite"></param>
    /// <param name="throwIfExists"></param>
    /// <returns></returns>
    Task<FileInfo> DownloadFileAsync(T model, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// Kintoneからファイルをダウンロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="overwrite"></param>
    /// <param name="throwIfExists"></param>
    /// <returns></returns>
    Task<FileInfo> DownloadFileAsync(T model, KintoneFile file, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// 複数のファイルをKintoneからダウンロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="files"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="overwrite"></param>
    /// <param name="throwIfExists"></param>
    /// <returns></returns>
    Task<IEnumerable<FileInfo>> DownloadFilesAsync(T model, IEnumerable<KintoneFile> files, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// Kintoneからファイルを指定パスにダウンロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <param name="savePath"></param>
    /// <param name="overwrite"></param>
    /// <param name="throwIfExists"></param>
    /// <returns></returns>
    Task<FileInfo> DownloadFileToPathAsync(T model, KintoneFile file, string savePath, bool overwrite = true, bool throwIfExists = false);
    #endregion
}
