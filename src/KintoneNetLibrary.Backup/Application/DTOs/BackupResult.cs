using KintoneNetLibrary.Backup.Application.Interfaces;

namespace KintoneNetLibrary.Backup.Application.DTOs;

public sealed class BackupResult : IOperationResult {
    /// <summary>
    /// バックアップ全体が成功したかどうか。
    /// 致命的エラーがなければ true。
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// JSON レコードの保存に成功したか。
    /// </summary>
    public bool JsonSaved { get; set; }

    /// <summary>
    /// フィールドスキーマの保存に成功したか。
    /// </summary>
    public bool SchemaSaved { get; set; }

    /// <summary>
    /// 保存したレコード数。
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// 添付ファイルのダウンロード成功数。
    /// </summary>
    public int FileDownloadedCount { get; set; }

    /// <summary>
    /// 添付ファイルのダウンロード失敗数。
    /// </summary>
    public int FileFailedCount { get; set; }

    /// <summary>
    /// スキップされた処理（例: 上書き禁止で既存ファイルがあった）。
    /// </summary>
    public List<string> Skipped { get; set; } = [];

    /// <summary>
    /// 警告（例: 一部ファイルのダウンロード失敗など）。
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// 致命的エラー（例外メッセージ）。
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// 成功だが警告あり、などを判定しやすくするための便利プロパティ。
    /// </summary>
    public bool HasWarnings => this.Warnings.Count > 0 || this.FileFailedCount > 0;

    /// <summary>
    /// 部分成功かどうか（Success が true だが警告あり）。
    /// </summary>
    public bool IsPartialSuccess => this.Success && this.HasWarnings;

    IReadOnlyList<string> IOperationResult.Warnings => this.Warnings;

    IReadOnlyList<string> IOperationResult.Errors => this.Errors;

    public int Parts { get; set; }
    public int SplitSize { get; set; }
}
