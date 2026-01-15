namespace KintoneNetLibrary.Backup.Models;

public sealed class BackupOptions {
    // ===== 必須 =====
    public required string SubDomain { get; init; }
    public required int AppID { get; init; }
    public required string ApiToken { get; init; }
    public required string OutputPath { get; init; }

    // ===== オプション =====
    public string? Query { get; init; }
    public IList<string>? FieldCodes { get; init; }

    /// <summary>
    /// 添付ファイルをダウンロードするかどうか
    /// </summary>
    public bool DownloadFiles { get; init; } = true;

    /// <summary>
    /// カーソルページサイズ（デフォルトは KintoneApi の CursorPageSize に合わせる）
    /// </summary>
    public int? BatchSize { get; init; }

    /// <summary>
    /// 既存ファイルを上書きするかどうか
    /// </summary>
    public bool Overwrite { get; init; } = false;

    /// <summary>
    /// フィールドスキーマを含めるかどうか
    /// </summary>
    public bool IncludeFieldSchema { get; init; } = true;

    /// <summary>
    /// フィールドスキーマのファイル名
    /// </summary>
    public string FieldSchemaFileName { get; init; } = "fields.json";

}
