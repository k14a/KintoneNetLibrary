using KintoneNetLibrary.Backup.Application.DTOs;

namespace KintoneNetLibrary.Backup.Application.Interfaces;

/// <summary>
/// リストアサービスのインターフェース
/// </summary>
public interface IRestoreService {
    /// <summary>
    /// リストアを実行します
    /// </summary>
    Task<RestoreResult> RunRestoreAsync();

    /// <summary>
    /// リストアのオプションを取得します
    /// </summary>
    RestoreOptions Options { get; set; }
}