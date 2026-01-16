namespace KintoneNetLibrary.Backup.Application.Interfaces;

/// <summary>
/// リストアサービスのインターフェース
/// </summary>
public interface IRestoreService {
    /// <summary>
    /// リストアを実行します
    /// </summary>
    Task RunRestoreAsync();
}