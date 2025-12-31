using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Extensions;

public static class KintoneTimeOnlyExtensions {
    /// <summary>
    /// TimeSpan → KintoneTimeOnly に変換（00:00～23:59 のみ許可）
    /// </summary>
    public static KintoneTimeOnly ToKintoneTimeOnly(this TimeSpan span) {
        if (span < TimeSpan.Zero || span >= TimeSpan.FromDays(1)) {
            throw new ArgumentOutOfRangeException(nameof(span), "Time must be between 00:00 and 23:59.");
        }

        return new KintoneTimeOnly(TimeOnly.FromTimeSpan(span));
    }

    /// <summary>
    /// "HH:mm" の文字列 → KintoneTimeOnly に変換
    /// </summary>
    public static KintoneTimeOnly ToKintoneTimeOnly(this string timeStr) {
        if (TimeOnly.TryParseExact(timeStr, "HH:mm", out var time)) {
            return new KintoneTimeOnly(time);
        }

        throw new FormatException("Invalid time format. Expected 'HH:mm'.");
    }

    /// <summary>
    /// KintoneTimeOnly を "HH:mm" 形式の文字列に変換
    /// </summary>
    public static string ToKintoneString(this KintoneTimeOnly kto) {
        return kto.Value?.ToString("HH:mm") ?? string.Empty;
    }

    /// <summary>
    /// DateOnly と結合して DateTime に変換（例: 2025-06-02 + 15:30 → 2025-06-02 15:30:00）
    /// </summary>
    public static DateTime ToDateTime(this KintoneTimeOnly kto, DateOnly baseDate) {
        if (!kto.Value.HasValue) {
            throw new InvalidOperationException("Cannot convert to DateTime because Value is null.");
        }
        return baseDate.ToDateTime(kto.Value.Value);
    }
}