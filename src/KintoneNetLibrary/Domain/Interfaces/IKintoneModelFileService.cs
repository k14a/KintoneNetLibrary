using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

public interface IKintoneModelFileService<T> where T : KintoneModelBase<T>, new() {
    #region <<Upload methods>>
    Task<KintoneFile> UploadFileAsync(T model);
    Task<KintoneFile> UploadFileAsync(T model, FileInfo file);
    Task<IEnumerable<KintoneFile>> UploadFilesAsync(T model, IEnumerable<FileInfo> files);
    Task MapUploadedFilesToModelAsync(T model, IEnumerable<FileInfo> files);
    #endregion

    #region <<Download methods>>
    Task<FileInfo> DownloadFileAsync(T model, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    Task<FileInfo> DownloadFileAsync(T model, KintoneFile file, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    Task<IEnumerable<FileInfo>> DownloadFilesAsync(T model, IEnumerable<KintoneFile> files, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    Task<FileInfo> DownloadFileToPathAsync(T model, KintoneFile file, string savePath, bool overwrite = true, bool throwIfExists = false);
    #endregion
}
