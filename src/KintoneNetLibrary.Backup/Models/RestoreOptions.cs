using KintoneNetLibrary.Backup.Enums;

namespace KintoneNetLibrary.Backup.Models;

public sealed class RestoreOptions {
    public required string SubDomain { get; init; }
    public required int AppID { get; init; }
    public required string ApiToken { get; init; }
    public required string BackupJsonPath { get; init; }

    public RestoreMode Mode { get; init; } = RestoreMode.FullReplace;
    public bool RestoreFiles { get; init; } = true;
    public bool Force { get; init; } = false; // スキーマ差異があっても強制復元
}
