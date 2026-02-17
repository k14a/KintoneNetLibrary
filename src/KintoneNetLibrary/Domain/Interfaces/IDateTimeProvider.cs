namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// 日付と時刻を提供するインターフェース
/// </summary>
public interface IDateTimeProvider {
    /// <summary>
    /// 現在のローカル日時を取得します。
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// 現在のUTC日時を取得します。
    /// </summary>
    DateTime UtcNow { get; }
}