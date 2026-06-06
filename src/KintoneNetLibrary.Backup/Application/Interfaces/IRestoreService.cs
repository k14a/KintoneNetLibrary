using KintoneNetLibrary.Backup.Application.DTOs;

namespace KintoneNetLibrary.Backup.Application.Interfaces;

/// <summary>
/// リストアサービスのインターフェース
/// </summary>
public interface IRestoreService {
    /// <summary>
    /// リストアを実行します
    /// </summary>
    /// <param name="options">リストアオプション</param>
    Task<RestoreResult> RunRestoreAsync(RestoreOptions options);
}