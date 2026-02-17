namespace KintoneNetLibrary.Backup.Application.DTOs;

/// <summary>
/// バックアップマニフェストクラス
/// </summary>
public class BackupManifest {
    /// <summary>
    /// アプリ情報
    /// </summary>
    public int AppId { get; set; }

    /// <summary>
    /// アプリのリビジョン番号
    /// </summary>
    public int AppRevision { get; set; }

    /// <summary>
    /// バックアップ実行日時
    /// </summary>
    public DateTime BackupAt { get; set; }

    // レコード情報
    /// <summary>
    /// レコード数
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// ファイルフィールド数
    /// </summary>
    public int FileFieldCount { get; set; }

    /// <summary>
    /// ファイル数
    /// </summary>
    public int FileCount { get; set; }

    // バックアップファイル
    /// <summary>
    /// バックアップファイルのリスト
    /// </summary>
    public IList<string> PartFiles { get; set; } = [];

    /// <summary>
    /// バックアップファイルの数（分割している場合は複数になる）
    /// </summary>
    public int Parts => this.PartFiles.Count;

    // バックアップ方式
    /// <summary>
    /// バックアップ方式
    /// </summary>
    public string BackupMode { get; set; } = "CursorPages";

    /// <summary>
    /// 分割サイズ
    /// </summary>
    public int SplitSize { get; set; }

    /// <summary>
    /// バックアップオプションの詳細（JSONシリアライズされた文字列）
    /// </summary>
    public object Options { get; set; } = default!;
}
