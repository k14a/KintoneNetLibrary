using KintoneNetLibrary.Backup.Application.Interfaces;

namespace KintoneNetLibrary.Backup.Application.DTOs;

/// <summary>
/// リストア結果クラス
/// </summary>
public sealed class RestoreResult : IOperationResult {
    /// <summary>
    /// リストア全体が成功したかどうか。
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// 追加されたレコード数。
    /// </summary>
    public int AddedRecords { get; set; }

    /// <summary>
    /// 更新されたレコード数。
    /// </summary>
    public int UpdatedRecords { get; set; }

    /// <summary>
    /// 削除されたレコード数。
    /// </summary>
    public int DeletedRecords { get; set; }

    /// <summary>
    /// 添付ファイルのアップロード成功数。
    /// </summary>
    public int UploadedFiles { get; set; }

    /// <summary>
    /// 添付ファイルのアップロード失敗数。
    /// </summary>
    public int FailedFiles { get; set; }

    /// <summary>
    /// スキップされた処理（例: Force=false でスキーマ差異があった）。
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// 致命的エラー（例外メッセージ）。
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// 警告があるかどうか。
    /// </summary>
    public bool HasWarnings => this.Warnings.Count > 0 || this.FailedFiles > 0;

    /// <summary>
    /// 致命的エラーがあるかどうか。
    /// </summary>
    public bool HasErrors => this.Errors.Count > 0 || !this.Success;

    /// <summary>
    /// 成功だが警告がある状態かどうか。
    /// </summary>
    public bool IsPartialSuccess => this.Success && this.HasWarnings;

    /// <summary>
    /// IOperationResult インターフェースの実装
    /// </summary>
    IReadOnlyList<string> IOperationResult.Warnings => this.Warnings;

    /// <summary>
    /// IOperationResult インターフェースの実装
    /// </summary>
    IReadOnlyList<string> IOperationResult.Errors => this.Errors;
}