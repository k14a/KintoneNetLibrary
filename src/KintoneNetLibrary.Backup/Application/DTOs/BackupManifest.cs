namespace KintoneNetLibrary.Backup.Application.DTOs;

public class BackupManifest {
    public int AppId { get; set; }
    public int AppRevision { get; set; }
    public int RecordCount { get; set; }
    public int FileFieldCount { get; set; }
    public int FileCount { get; set; }
    public DateTime BackupAt { get; set; }
    public object Options { get; set; } = default!;
}
