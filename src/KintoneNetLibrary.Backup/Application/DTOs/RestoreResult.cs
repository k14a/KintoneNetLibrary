using KintoneNetLibrary.Backup.Application.Interfaces;

namespace KintoneNetLibrary.Backup.Application.DTOs;

public sealed class RestoreResult : IOperationResult {
    public bool Success { get; set; } = true;

    public int AddedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int DeletedRecords { get; set; }

    public int UploadedFiles { get; set; }
    public int FailedFiles { get; set; }

    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];

    public bool HasWarnings => this.Warnings.Count > 0 || this.FailedFiles > 0;
    public bool HasErrors => this.Errors.Count > 0 || !this.Success;

    public bool IsPartialSuccess => this.Success && this.HasWarnings;

    IReadOnlyList<string> IOperationResult.Warnings => this.Warnings;

    IReadOnlyList<string> IOperationResult.Errors => this.Errors;
}