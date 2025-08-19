using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Application.UseCases.Services;

public class KintoneModelFileService<T> : IKintoneModelFileService<T> where T : KintoneModelBase<T>, new() {
    private readonly IKintoneRepository _repository;
    private readonly ILogger<KintoneModelFileService<T>>? _logger;

    public KintoneModelFileService(IKintoneRepository repository, ILogger<KintoneModelFileService<T>>? logger = null) {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._logger = logger;
    }

    #region <<Upload methods>>
    public async Task<KintoneFile> UploadFileAsync(T model) {
        ArgumentNullException.ThrowIfNull(model);

        var props = typeof(T).GetProperties();

        var fileInfoProp = props.FirstOrDefault(p => p.PropertyType == typeof(FileInfo));
        var kintoneFileProp = props.FirstOrDefault(p => p.PropertyType == typeof(KintoneFile));

        if (fileInfoProp == null || kintoneFileProp == null) {
            throw new InvalidOperationException($"モデル '{typeof(T).Name}' に FileInfo と KintoneFile の両方のプロパティが必要です。");
        }

        var fileInfo = fileInfoProp.GetValue(model) as FileInfo;
        if (fileInfo == null || !fileInfo.Exists) {
            throw new FileNotFoundException("アップロード対象のファイルが存在しません。", fileInfo?.FullName);
        }

        var fileKey = await this._repository.UploadFileAsync(model, fileInfo);

        if (kintoneFileProp.GetValue(model) is not KintoneFile kf) {
            kf = new KintoneFile();
            kintoneFileProp.SetValue(model, kf);
        }

        kf.FileKey = fileKey;
        kf.Name = fileInfo.Name;
        kf.Size = fileInfo.Length;
        kf.ContentType = MimeTypes.GetMimeType(fileInfo.Name) ?? "application/octet-stream"; // MIME タイプの推定

        this._logger?.LogInformation("ファイル '{FileName}' をアップロードし、FileKey をモデルに設定しました。", fileInfo.Name);

        return kf;
    }
    public async Task<KintoneFile> UploadFileAsync(T model, FileInfo file) {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(file);
        if (!file.Exists) {
            throw new FileNotFoundException("アップロード対象のファイルが存在しません。", file.FullName);
        }

        var fileKey = await this._repository.UploadFileAsync(model, file);

        var kf = new KintoneFile {
            FileKey = fileKey,
            Name = file.Name,
            Size = file.Length,
            ContentType = MimeTypes.GetMimeType(file.Name) ?? "application/octet-stream" // MIME タイプの推定
        };

        this._logger?.LogInformation("ファイル '{FileName}' をアップロードしました。FileKey: {FileKey}", file.Name, fileKey);

        return kf;
    }
    public async Task<List<KintoneFile>> UploadFilesAsync(T model) {
        ArgumentNullException.ThrowIfNull(model);

        var props = typeof(T).GetProperties();
        var fileListProp = props.FirstOrDefault(p => p.PropertyType == typeof(List<FileInfo>));
        var kintoneFileListProp = props.FirstOrDefault(p => p.PropertyType == typeof(List<KintoneFile>));

        if (fileListProp == null || kintoneFileListProp == null) {
            throw new InvalidOperationException($"モデル '{typeof(T).Name}' に List<FileInfo> と List<KintoneFile> の両方のプロパティが必要です。");
        }

        if (fileListProp.GetValue(model) is not List<FileInfo> fileList || fileList.Count == 0) {
            throw new FileNotFoundException("アップロード対象のファイルが存在しません。");
        }

        var resultList = new List<KintoneFile>();
        foreach (var file in fileList) {
            if (!file.Exists) { continue; }

            var fileKey = await this._repository.UploadFileAsync(model, file);
            var kf = new KintoneFile {
                FileKey = fileKey,
                Name = file.Name,
                Size = file.Length,
                ContentType = MimeTypes.GetMimeType(file.Name) ?? "application/octet-stream"
            };

            resultList.Add(kf);
            this._logger?.LogInformation("ファイル '{FileName}' をアップロードしました。", file.Name);
        }

        kintoneFileListProp.SetValue(model, resultList);
        return resultList;
    }

    public async Task<IEnumerable<KintoneFile>> UploadFilesAsync(T model, IEnumerable<FileInfo> files) {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(files);

        var result = new List<KintoneFile>();

        foreach (var file in files) {
            if (!file.Exists) {
                this._logger?.LogWarning("アップロード対象のファイルが存在しません: {Path}", file.FullName);
                continue;
            }

            try {
                var kf = await this.UploadFileAsync(model, file);
                result.Add(kf);
            } catch (Exception ex) {
                this._logger?.LogError(ex, "ファイル '{FileName}' のアップロードに失敗しました。", file.Name);
                // 必要に応じて throw か continue を選択可能（現状は continue）
            }
        }

        return result;
    }
    public Task MapUploadedFilesToModelAsync(T model, IEnumerable<FileInfo> files) {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(files);

        var fileList = files.ToList();
        var props = typeof(T).GetProperties();

        foreach (var prop in props) {
            if (prop.PropertyType == typeof(KintoneFile)) {
                if (prop.GetValue(model) is not KintoneFile kf) {
                    continue;
                }

                var match = fileList.FirstOrDefault(f => string.Equals(f.Name, kf.Name, StringComparison.OrdinalIgnoreCase));
                if (match != null && string.IsNullOrEmpty(kf.FileKey)) {
                    this._logger?.LogInformation("File '{FileName}' をプロパティ '{PropName}' にマッピングしました。", match.Name, prop.Name);
                    kf.FileKey = "[UPLOADED]"; // 実際は UploadFileAsync() の戻り値でセット済みの想定
                }

            } else if (typeof(IEnumerable<KintoneFile>).IsAssignableFrom(prop.PropertyType)) {
                if (prop.GetValue(model) is not IEnumerable<KintoneFile> list) {
                    continue;
                }

                foreach (var kf in list) {
                    var match = fileList.FirstOrDefault(f => string.Equals(f.Name, kf.Name, StringComparison.OrdinalIgnoreCase));
                    if (match != null && string.IsNullOrEmpty(kf.FileKey)) {
                        this._logger?.LogInformation("File '{FileName}' をリスト内の KintoneFile にマッピングしました。", match.Name);
                        kf.FileKey = "[UPLOADED]";
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    #endregion

    #region <<Download methods>>
    public async Task<FileInfo> DownloadFileAsync(T model, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false) {
        // モデル内の KintoneFile を探索（単一ファイルを想定）
        var fileProp = typeof(T).GetProperties().FirstOrDefault(p => p.PropertyType == typeof(KintoneFile)) ?? throw new InvalidOperationException($"Model '{typeof(T).Name}' に KintoneFile 型のプロパティが見つかりません。");
        if (fileProp.GetValue(model) is not KintoneFile file || string.IsNullOrEmpty(file.FileKey)) {
            throw new ArgumentException("モデルに有効な KintoneFile が設定されていません。");
        }

        // ファイルバイト列を取得
        var bytes = await this._repository.DownloadFileAsync(model, file.Name);
        return await this.SaveFileAsync(file.Name, bytes, targetDirectory, overwrite, throwIfExists);
    }
    public async Task<IEnumerable<FileInfo>> DownloadFilesAsync(T model, IEnumerable<KintoneFile> files, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false) {
        var result = new List<FileInfo>();
        var folder = targetDirectory ?? Path.GetTempPath();

        foreach (var file in files) {
            if (string.IsNullOrEmpty(file.FileKey)) {
                this._logger?.LogWarning("FileKeyが未設定のファイルをスキップしました: {FileName}", file.Name);
                continue;
            }

            try {
                var bytes = await this._repository.DownloadFileAsync(model, file.Name);
                var saved = await this.SaveFileAsync(file.Name, bytes, targetDirectory, overwrite, throwIfExists);
                result.Add(saved);

            } catch (Exception ex) {
                this._logger?.LogError(ex, "ファイルのダウンロードに失敗しました: {FileName}", file.Name);
                // 必要に応じて continue か throw を選択可能（現状は continue）
            }
        }

        return result;
    }
    public async Task<FileInfo> DownloadFileAsync(T model, KintoneFile file, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false) {
        if (string.IsNullOrEmpty(file.FileKey)) {
            throw new ArgumentException("FileKeyが未設定のファイルはダウンロードできません。", nameof(file));
        }

        var bytes = await this._repository.DownloadFileAsync(model, file.Name);
        return await this.SaveFileAsync(file.Name, bytes, targetDirectory, overwrite, throwIfExists);
    }
    public async Task<FileInfo> DownloadFileToPathAsync(T model, KintoneFile file, string savePath, bool overwrite = true, bool throwIfExists = false) {
        if (string.IsNullOrEmpty(file.FileKey)) {
            throw new ArgumentException("FileKeyが未設定のファイルはダウンロードできません。", nameof(file));
        }

        // ファイルバイト列を取得
        var bytes = await this._repository.DownloadFileAsync(model, file.Name);

        // 保存処理（共通化されたメソッドを使用）
        return await this.SaveFileToPathAsync(savePath, bytes, overwrite, throwIfExists);
    }


    private async Task<FileInfo> SaveFileAsync(string fileName, byte[] content, string? targetDirectory, bool overwrite, bool throwIfExists) {
        var folder = targetDirectory ?? Path.GetTempPath();
        var filePath = Path.Combine(folder, fileName);

        return await this.SaveFileToPathAsync(filePath, content, overwrite, throwIfExists);
    }
    private async Task<FileInfo> SaveFileToPathAsync(string savePath, byte[] content, bool overwrite, bool throwIfExists) {
        if (File.Exists(savePath)) {
            if (overwrite) {
                // OK
            } else if (throwIfExists) {
                throw new IOException($"保存先に既にファイルが存在します: {savePath}");
            } else {
                var dir = Path.GetDirectoryName(savePath)!;
                var baseName = Path.GetFileNameWithoutExtension(savePath);
                var ext = Path.GetExtension(savePath);
                int suffix = 1;
                while (File.Exists(savePath)) {
                    savePath = Path.Combine(dir, $"{baseName}_{suffix++}{ext}");
                }
                this._logger?.LogWarning("既存ファイルが存在したため、ファイル名を変更して保存しました: {Path}", savePath);
            }
        }

        await File.WriteAllBytesAsync(savePath, content);
        this._logger?.LogInformation("ファイルを保存しました: {Path}", savePath);
        return new FileInfo(savePath);
    }

    #endregion
}
