namespace KintoneNetLibrary.Backup.Application.DTOs;

/// <summary>
/// バックアップオプションクラス
/// </summary>
public sealed class BackupOptions {
    /// <summary>
    /// Kintone サブドメイン(必須)
    /// </summary>
    public required string SubDomain { get; init; }

    /// <summary>
    /// アプリID(必須)
    /// </summary>
    public required int AppID { get; init; }

    /// <summary>
    /// APIトークン(必須)
    /// </summary>
    public required string ApiToken { get; init; }

    /// <summary>
    /// 出力先パス(必須)
    /// </summary>
    public required DirectoryInfo OutputPath { get; init; }

    /// <summary>
    /// クエリ文字列
    /// NULL の場合、全レコードを取得します
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// 取得するフィールドコードのリスト
    /// null の場合は全フィールドを取得します
    /// </summary>
    public IList<string>? FieldCodes { get; init; }

    /// <summary>
    /// 添付ファイルをダウンロードするかどうか
    /// false の場合、添付ファイルフィールドは空のまま取得されます
    /// </summary>
    public bool DownloadFiles { get; init; } = true;

    /// <summary>
    /// カーソルページサイズ（デフォルトは KintoneApi の CursorPageSize に合わせる）
    /// </summary>
    public int? BatchSize { get; init; }

    /// <summary>
    /// 既存ファイルを上書きするかどうか
    /// false の場合、既存ファイルが存在するときはスキップします
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

    /// <summary>
    /// JSONファイルの整形出力を行うかどうか
    /// </summary>
    public bool Pretty { get; init; } = false;

    /// <summary>
    /// JSONファイルのUnicodeエスケープを行うかどうか
    /// </summary>
    public bool EscapeUnicode { get; init; } = true;

    /// <summary>
    /// データファイルを分割するレコード数
    /// </summary>
    public int SplitSize { get; init; } = 1000;
}
