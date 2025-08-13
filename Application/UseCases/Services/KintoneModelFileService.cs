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
    public Task MapUploadedFilesToModelAsync(T model, IEnumerable<FileInfo> files) {
        throw new NotImplementedException();
    }

    public Task<KintoneFile> UploadFileAsync(T model) {
        throw new NotImplementedException();
    }

    public Task<KintoneFile> UploadFileAsync(T model, FileInfo file) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<KintoneFile>> UploadFilesAsync(T model, IEnumerable<FileInfo> files) {
        throw new NotImplementedException();
    }
    #endregion

    #region <<Download methods>>
    public async Task<FileInfo> DownloadFileAsync(T model, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false) {
        // モデル内の KintoneFile を探索（単一ファイルを想定）
        var fileProp = typeof(T).GetProperties().FirstOrDefault(p => p.PropertyType == typeof(KintoneFile));

        if (fileProp == null) {
            throw new InvalidOperationException($"Model '{typeof(T).Name}' に KintoneFile 型のプロパティが見つかりません。");
        }

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
