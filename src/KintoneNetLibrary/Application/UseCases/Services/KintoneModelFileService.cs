using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Application.UseCases.Services;

/// <summary>
/// Kintoneモデルのファイル操作を提供するサービスクラス。
/// </summary>
/// <remarks>このクラスは、Kintoneモデルのファイルアップロードとダウンロードを管理します。</remarks>
/// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
/// <param name="repository">Kintoneリポジトリインターフェース</param>
/// <param name="logger">ロガーインスタンス（オプション）</param>
/// <exception cref="ArgumentNullException">repositoryがnullの場合にスローされます。</exception>
public class KintoneModelFileService<T>(IKintoneRepository repository, ILogger<KintoneModelFileService<T>>? logger = null) : IKintoneModelFileService<T> where T : KintoneModelBase<T>, new() {
    private readonly IKintoneRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<KintoneModelFileService<T>>? _logger = logger;

    #region <<Upload methods>>
    /// <summary>
    /// モデルのファイルをアップロードし、KintoneFileを更新します。
    /// </summary>
    /// <remarks>モデル内のFileInfoプロパティを使用して、ファイルをアップロードします。</remarks>
    /// <param name="model">アップロード対象のKintoneモデル</param>
    /// <returns>アップロードされたKintoneFileオブジェクト</returns>
    /// <exception cref="ArgumentNullException">modelがnullの場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">モデルにFileInfoとKintoneFileの両方のプロパティが必要です。</exception>
    /// <exception cref="FileNotFoundException">アップロード対象のファイルが存在しない場合にスローされます。</exception>
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

    /// <summary>
    /// モデルのファイルをアップロードし、KintoneFileのリストを更新します。
    /// </summary>
    /// <remarks>モデル内のFileInfoリストを使用して、複数のファイルをアップロードします。</remarks>
    /// <param name="model">アップロード対象のKintoneモデル</param>
    /// <returns>アップロードされたKintoneFileのリスト</returns>
    /// <exception cref="ArgumentNullException">modelがnullの場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">モデルにFileInfoとKintoneFileの両方のプロパティが必要です。</exception>
    /// <exception cref="FileNotFoundException">アップロード対象のファイルが存在しない場合にスローされます。</exception>
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

    /// <summary>
    /// モデルのファイルをアップロードし、KintoneFileのリストを更新します。
    /// </summary>
    /// <remarks>モデル内のFileInfoリストを使用して、複数のファイルをアップロードします。</remarks>
    /// <param name="model">アップロード対象のKintoneモデル</param>
    /// <returns>アップロードされたKintoneFileのリスト</returns>
    /// <exception cref="ArgumentNullException">modelがnullの場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">モデルにFileInfoとKintoneFileの両方のプロパティが必要です。</exception>
    /// <exception cref="FileNotFoundException">アップロード対象のファイルが存在しない場合にスローされます。</exception>
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

    /// <summary>
    /// モデルのファイルをアップロードし、KintoneFileのリストを更新します。
    /// </summary>
    /// <remarks>モデル内のFileInfoリストを使用して、複数のファイルをアップロードします。</remarks>
    /// <param name="model">アップロード対象のKintoneモデル</param>
    /// <param name="files">アップロードするFileInfoのリスト</param>
    /// <returns>アップロードされたKintoneFileのリスト</returns>
    /// <exception cref="ArgumentNullException">modelまたはfilesがnullの場合にスローされます。</exception>
    /// <exception cref="FileNotFoundException">アップロード対象のファイルが存在しない場合にスローされます。</exception>
    /// <remarks>アップロードされたファイルは、モデルのKintoneFileプロパティにマッピングされます。</remarks>
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

    /// <summary>
    /// アップロードされたファイルをモデルのKintoneFileプロパティにマッピングします。
    /// </summary>
    /// <remarks>アップロードされたファイルは、モデルのKintoneFileプロパティにマッピングされます。</remarks>
    /// <param name="model">アップロードされたファイルをマッピングするKintoneモデル</param>
    /// <param name="files">アップロードされたFileInfoのリスト</param>
    /// <returns>非同期タスク</returns>
    /// <exception cref="ArgumentNullException">modelまたはfilesがnullの場合にスローされます。</exception>
    /// <remarks>アップロードされたファイルは、モデルのKintoneFileプロパティにマッピングされます。</remarks>
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
    /// <summary>
    /// モデルのファイルをダウンロードし、FileInfoを返します。
    /// </summary>
    /// <remarks>モデル内のKintoneFileプロパティを使用して、ファイルをダウンロードします。</remarks>
    /// <param name="model">ダウンロード対象のKintoneモデル</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ（nullの場合は一時ディレクトリを使用）</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルのFileInfo</returns>
    /// <exception cref="ArgumentNullException">modelがnullの場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">モデルにKintoneFileプロパティが存在しない場合にスローされます。</exception>
    /// <exception cref="ArgumentException">モデルに有効なKintoneFileが設定されていない場合にスローされます。</exception>
    /// <exception cref="FileNotFoundException">ダウンロード対象のファイルが存在しない場合にスローされます。</exception>
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

    /// <summary>
    /// モデルのファイルをダウンロードし、FileInfoのリストを返します。
    /// </summary>
    /// <remarks>モデル内のKintoneFileリストを使用して、複数のファイルをダウンロードします。</remarks>
    /// <param name="model">ダウンロード対象のKintoneモデル</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ（nullの場合は一時ディレクトリを使用）</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルのFileInfoのリスト</returns>
    /// <exception cref="ArgumentNullException">modelがnullの場合にスローされます。</exception>
    /// <exception cref="InvalidOperationException">モデルにKintoneFileリストプロパティが存在しない場合にスローされます。</exception>
    /// <exception cref="ArgumentException">モデルに有効なKintoneFileが設定されていない場合にスローされます。</exception>
    /// <exception cref="FileNotFoundException">ダウンロード対象のファイルが存在しない場合にスローされます。</exception>
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

    /// <summary>
    /// モデルのファイルをダウンロードし、指定されたパスに保存します。
    /// </summary>
    /// <remarks>モデル内のKintoneFileを使用して、ファイルをダウンロードし、指定されたパスに保存します。</remarks>
    /// <param name="model">ダウンロード対象のKintoneモデル</param>
    /// <param name="file">ダウンロードするKintoneFile</param>  
    /// <param name="overwrite">既存ファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルのFileInfo</returns>
    /// <exception cref="ArgumentNullException">modelまたはfileがnullの場合にスローされます。</exception>
    /// <exception cref="ArgumentException">fileのFileKeyが未設定の場合にスローされます。</exception>
    /// <exception cref="FileNotFoundException">ダウンロード対象のファイルが存在しない場合にスローされます。</exception>
    public async Task<FileInfo> DownloadFileAsync(T model, KintoneFile file, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false) {
        if (string.IsNullOrEmpty(file.FileKey)) {
            throw new ArgumentException("FileKeyが未設定のファイルはダウンロードできません。", nameof(file));
        }

        var bytes = await this._repository.DownloadFileAsync(model, file.Name);
        return await this.SaveFileAsync(file.Name, bytes, targetDirectory, overwrite, throwIfExists);
    }

    /// <summary>
    /// モデルのファイルをダウンロードし、指定されたパスに保存します。
    /// </summary>
    /// <remarks>モデル内のKintoneFileを使用して、ファイルをダウンロードし、指定されたパスに保存します。</remarks>
    /// <param name="model">ダウンロード対象のKintoneモデル</param>
    /// <param name="file">ダウンロードするKintoneFile</param>
    /// <param name="savePath">保存先のパス</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルのFileInfo</returns>
    /// <exception cref="ArgumentNullException">modelまたはfileがnullの場合にスローされます。</exception>
    /// <exception cref="ArgumentException">fileのFileKeyが未設定の場合にスローされます。</exception>
    /// <exception cref="FileNotFoundException">ダウンロード対象のファイルが存在しない場合にスローされます。</exception>
    public async Task<FileInfo> DownloadFileToPathAsync(T model, KintoneFile file, string savePath, bool overwrite = true, bool throwIfExists = false) {
        if (string.IsNullOrEmpty(file.FileKey)) {
            throw new ArgumentException("FileKeyが未設定のファイルはダウンロードできません。", nameof(file));
        }

        // ファイルバイト列を取得
        var bytes = await this._repository.DownloadFileAsync(model, file.Name);

        // 保存処理（共通化されたメソッドを使用）
        return await this.SaveFileToPathAsync(savePath, bytes, overwrite, throwIfExists);
    }

    /// <summary>
    /// 指定されたファイル名でファイルを保存します。    
    /// </summary>
    /// <remarks>ファイル名、バイト配列、保存先ディレクトリ、上書きオプション、既存ファイルの存在時の挙動を指定してファイルを保存します。</remarks>
    /// <param name="fileName">保存するファイルの名前</param>
    /// <param name="content">ファイルのバイト配列</param>
    /// <param name="targetDirectory">保存先のディレクトリ（nullの場合は一時ディレクトリを使用）</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>保存されたファイルのFileInfo</returns>
    /// <exception cref="ArgumentNullException">fileNameまたはcontentがnullの場合にスローされます。</exception>
    /// <exception cref="IOException">保存先に既にファイルが存在する場合にスローされます。</exception>
    private async Task<FileInfo> SaveFileAsync(string fileName, byte[] content, string? targetDirectory, bool overwrite, bool throwIfExists) {
        var folder = targetDirectory ?? Path.GetTempPath();
        var filePath = Path.Combine(folder, fileName);

        return await this.SaveFileToPathAsync(filePath, content, overwrite, throwIfExists);
    }

    /// <summary>
    /// 指定されたパスにファイルを保存します。
    /// </summary>
    /// <remarks>ファイルのパス、バイト配列、上書きオプション、既存ファイルの存在時の挙動を指定してファイルを保存します。</remarks>
    /// <param name="savePath">保存先のパス</param>
    /// <param name="content">ファイルのバイト配列</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存ファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>保存されたファイルのFileInfo</returns>
    /// <exception cref="ArgumentNullException">savePathまたはcontentがnullの場合にスローされます。</exception>
    /// <exception cref="IOException">保存先に既にファイルが存在する場合にスローされます。</exception>
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
