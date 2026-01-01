using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintoneモデルのファイル操作を提供するサービスインターフェイスです。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKintoneModelFileService<T> where T : KintoneModelBase<T>, new() {
    /// <summary>
    /// ファイルをアップロードします。
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    Task<string> UploadFileAsync(string filePath);
    /// <summary>
    /// ファイルをアップロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<KintoneFile> UploadFileAsync(T model, FileInfo file);
    /// <summary>
    /// 複数ファイルをアップロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<List<KintoneFile>> UploadFilesAsync(T model);
    /// <summary>
    /// 複数ファイルをアップロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    Task<IEnumerable<KintoneFile>> UploadFilesAsync(T model, IEnumerable<FileInfo> files);
    /// <summary>
    /// アップロードされたファイルをモデルにマッピングします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    Task MapUploadedFilesToModelAsync(T model, IEnumerable<FileInfo> files);

    /// <summary>
    /// ファイルをダウンロードします。
    /// </summary>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    Task<byte[]> DownloadFileAsync(string fileKey);
    /// <summary>
    /// ファイルをダウンロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="files"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="overwrite"></param>
    /// <param name="throwIfExists"></param>
    /// <returns></returns>
    Task<IEnumerable<FileInfo>> DownloadFilesAsync(T model, IEnumerable<KintoneFile> files, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// ファイルをダウンロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="overwrite"></param>
    /// <param name="throwIfExists"></param>
    /// <returns></returns>
    Task<FileInfo> DownloadFileAsync(T model, KintoneFile file, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// ファイルを指定パスにダウンロードします。
    /// </summary>
    /// <param name="model"></param>
    /// <param name="file"></param>
    /// <param name="savePath"></param>
    /// <param name="overwrite"></param>
    /// <param name="throwIfExists"></param>
    /// <returns></returns>
    Task<FileInfo> DownloadFileToPathAsync(T model, KintoneFile file, string savePath, bool overwrite = true, bool throwIfExists = false);

}