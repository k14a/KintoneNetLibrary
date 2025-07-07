namespace KintoneNetLibrary.Domain.Common;

public class KintoneExecutionOptions {
    public int MaxConcurrency { get; set; } = KintoneConstants.MaxConcurrentRequestCount;
    public bool EnableSingleRetryOnError { get; set; } = true;
    public bool EnableCreateToUpdateRetry { get; set; } = true;
}
