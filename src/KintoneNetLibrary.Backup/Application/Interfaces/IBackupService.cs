using KintoneNetLibrary.Backup.Application.DTOs;

namespace KintoneNetLibrary.Backup.Application.Interfaces;

/// <summary>
/// バックアップサービスのインターフェース
/// </summary>
public interface IBackupService {
    /// <summary>
    /// バックアップを実行します
    /// </summary>
    /// <param name="options">バックアップオプション</param>
    Task<BackupResult> RunBackupAsync(BackupOptions options);
}