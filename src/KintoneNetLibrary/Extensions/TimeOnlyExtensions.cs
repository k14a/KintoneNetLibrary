namespace KintoneNetLibrary.Extensions;

// コメントは日本語で記述
/// <summary>
/// TimeOnly型の拡張メソッドを提供します。
/// </summary>
public static class TimeOnlyExtensions {
    /// <summary>
    /// 秒・ミリ秒を切り捨てて、時・分のみを保持します。
    /// </summary>
    public static TimeOnly TruncateToMinute(this TimeOnly time) {
        return new TimeOnly(time.Hour, time.Minute);
    }

    /// <summary>
    /// ミリ秒を切り捨てて、時・分・秒のみを保持します。
    /// </summary>
    public static TimeOnly TruncateToSecond(this TimeOnly time) {
        return new TimeOnly(time.Hour, time.Minute, time.Second);
    }
}
