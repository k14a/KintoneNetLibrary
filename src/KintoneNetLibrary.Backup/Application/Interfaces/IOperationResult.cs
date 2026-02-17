namespace KintoneNetLibrary.Backup.Application.Interfaces;

/// <summary>
/// 操作結果のインターフェース
/// </summary>
public interface IOperationResult {
    /// <summary>
    /// 操作全体が成功したかどうか。
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// 警告のリスト（例: スキップされた処理の説明）。
    /// </summary>
    IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// 致命的エラーのリスト（例: 例外メッセージ）。
    /// </summary>
    IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// 部分成功の概念を統一
    /// </summary>
    bool IsPartialSuccess { get; }
}
