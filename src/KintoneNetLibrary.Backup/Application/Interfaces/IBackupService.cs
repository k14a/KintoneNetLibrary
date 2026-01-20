using KintoneNetLibrary.Backup.Application.DTOs;

namespace KintoneNetLibrary.Backup.Application.Interfaces;

/// <summary>
/// バックアップサービスのインターフェース
/// </summary>
public interface IBackupService {
    /// <summary>
    /// バックアップを実行します
    /// </summary>
    Task<BackupResult> RunBackupAsync();
}