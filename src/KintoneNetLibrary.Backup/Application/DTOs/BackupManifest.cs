namespace KintoneNetLibrary.Backup.Application.DTOs;

public class BackupManifest {
    public int AppId { get; set; }
    public int AppRevision { get; set; }
    public DateTime BackupAt { get; set; }

    // レコード情報
    public int RecordCount { get; set; }
    public int FileFieldCount { get; set; }
    public int FileCount { get; set; }

    // バックアップファイル
    public IList<string> PartFiles { get; set; } = [];
    public int Parts => this.PartFiles.Count;

    // バックアップ方式
    public string BackupMode { get; set; } = "CursorPages";

    public int SplitSize { get; set; }
    public object Options { get; set; } = default!;
}
