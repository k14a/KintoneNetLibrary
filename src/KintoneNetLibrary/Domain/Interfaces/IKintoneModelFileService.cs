using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// Kintoneのファイル操作サービスのインターフェース
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKintoneModelFileService<T> where T : KintoneModelBase<T>, new() {
    #region <<Upload methods>>
    /// <summary>
    /// ファイルをKintoneにアップロードします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <returns>アップロードされたファイルの情報を含むKintoneFile</returns>
    Task<KintoneFile> UploadFileAsync(T model);
    /// <summary>
    /// ファイルをKintoneにアップロードします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="file">アップロードするファイルの情報を含むFileInfo</param>
    /// <returns>アップロードされたファイルの情報を含むKintoneFile</returns>
    Task<KintoneFile> UploadFileAsync(T model, FileInfo file);
    /// <summary>
    /// 複数のファイルをKintoneにアップロードします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="files">アップロードするファイルの情報を含むFileInfoのリスト</param>
    /// <returns>アップロードされたファイルの情報を含むKintoneFileのリスト</returns>
    Task<IEnumerable<KintoneFile>> UploadFilesAsync(T model, IEnumerable<FileInfo> files);
    /// <summary>
    /// アップロードしたファイルをモデルにマッピングします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="files">アップロードされたファイルの情報を含むFileInfoのリスト</param>
    /// <returns>マッピング結果を示すタスク</returns>
    Task MapUploadedFilesToModelAsync(T model, IEnumerable<FileInfo> files);
    #endregion

    #region <<Download methods>>
    /// <summary>
    /// Kintoneからファイルをダウンロードします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ</param>
    /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存のファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルの情報を含むFileInfo</returns>
    Task<FileInfo> DownloadFileAsync(T model, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// Kintoneからファイルをダウンロードします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="file">ダウンロードするファイルの情報を含むKintoneFile</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ</param>
    /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存のファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルの情報を含むFileInfo</returns>
    Task<FileInfo> DownloadFileAsync(T model, KintoneFile file, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// 複数のファイルをKintoneからダウンロードします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="files">ダウンロードするファイルの情報を含むKintoneFileのリスト</param>
    /// <param name="targetDirectory">ダウンロード先のディレクトリ</param>
    /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存のファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルの情報を含むFileInfoのリスト</returns>
    Task<IEnumerable<FileInfo>> DownloadFilesAsync(T model, IEnumerable<KintoneFile> files, string? targetDirectory = null, bool overwrite = true, bool throwIfExists = false);
    /// <summary>
    /// Kintoneからファイルを指定パスにダウンロードします。
    /// </summary>
    /// <param name="model">Kintoneモデルのインスタンス</param>
    /// <param name="file">ダウンロードするファイルの情報を含むKintoneFile</param>
    /// <param name="savePath">保存先のパス</param>
    /// <param name="overwrite">既存のファイルを上書きするかどうか</param>
    /// <param name="throwIfExists">既存のファイルが存在する場合に例外をスローするかどうか</param>
    /// <returns>ダウンロードされたファイルの情報を含むFileInfo</returns>
    Task<FileInfo> DownloadFileToPathAsync(T model, KintoneFile file, string savePath, bool overwrite = true, bool throwIfExists = false);
    #endregion
}
