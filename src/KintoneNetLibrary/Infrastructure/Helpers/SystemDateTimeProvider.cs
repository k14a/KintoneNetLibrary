using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Helpers;

/// <summary>
/// システムの日時を提供するためのクラス
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider {
    /// <summary>
    /// 現在のシステム日時を取得します
    /// </summary>
    public DateTime Now => DateTime.Now;

    /// <summary>
    /// 現在のシステム日時を UTC 形式で取得します
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}