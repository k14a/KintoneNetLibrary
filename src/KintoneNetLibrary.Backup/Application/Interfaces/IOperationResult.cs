namespace KintoneNetLibrary.Backup.Application.Interfaces;

public interface IOperationResult {
    bool Success { get; }
    IReadOnlyList<string> Warnings { get; }
    IReadOnlyList<string> Errors { get; }

    // 部分成功の概念を統一
    bool IsPartialSuccess { get; }
}
