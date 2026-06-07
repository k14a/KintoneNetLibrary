using KintoneNetLibrary.Backup.Domain.Enums;

namespace KintoneNetLibrary.Backup.Application.DTOs;

/// <summary>
/// リストアオプション
/// </summary>
public sealed class RestoreOptions {
    /// <summary>
    /// Kintone サブドメイン(必須)
    /// </summary>
    public required string SubDomain { get; init; }

    /// <summary>
    /// アプリId(必須)
    /// </summary>
    public required int AppId { get; init; }

    /// <summary>
    /// APIトークン(必須)
    /// </summary>
    public required string ApiToken { get; init; }

    /// <summary>
    /// バックアップJSONファイルのパス(必須)
    /// </summary>
    public required DirectoryInfo BackupRootPath { get; init; }

    /// <summary>
    /// リストアモード
    /// </summary>
    public RestoreMode Mode { get; init; } = RestoreMode.FullReplace;

    /// <summary>
    /// 添付ファイルをリストアするかどうか
    /// false の場合、添付ファイルフィールドは空のまま復元されます
    /// </summary>
    public bool RestoreFiles { get; init; } = true;

    /// <summary>
    /// スキーマ差異があっても強制復元するかどうか
    /// </summary>
    public bool Force { get; init; } = false;

    /// <summary>
    /// ドライランモードかどうか
    /// </summary>
    public bool DryRun { get; init; } = false;

    /// <summary>
    /// バリデーションのみ実行するかどうか
    /// </summary>
    public bool ValidateOnly { get; init; } = false;

    /// <summary>
    /// バッチサイズ（デフォルトは Kintone 一括登録上限の 100 件）
    /// </summary>
    public int BatchSize { get; init; } = 100;
}
