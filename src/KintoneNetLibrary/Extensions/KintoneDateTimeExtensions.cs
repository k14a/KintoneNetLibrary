namespace KintoneNetLibrary.Extensions;

using KintoneNetLibrary.Domain.Entities;

public static class KintoneDateTimeExtensions {
    /// <summary>
    /// KintoneDateTime を ISO 8601 の "yyyy-MM-ddTHH:mm" 形式で文字列化（Kintone仕様に準拠）
    /// </summary>
    public static string ToKintoneString(this KintoneDateTime kdt) {
        return kdt.Value.ToString("yyyy-MM-ddTHH:mm");
    }

    /// <summary>
    /// 指定のフォーマットで文字列化
    /// </summary>
    public static string ToKintoneString(this KintoneDateTime kdt, KintoneFieldType type) {
        return type switch {
            KintoneFieldType.Date => kdt.Value.ToString("yyyy-MM-dd"),
            KintoneFieldType.DateTime => kdt.Value.ToString("yyyy-MM-ddTHH:mm"),
            _ => throw new NotSupportedException($"Unsupported DateTimeType: {type}")
        };
    }

    /// <summary>
    /// ISO文字列から KintoneDateTime に変換
    /// </summary>
    public static KintoneDateTime ToKintoneDateTime(this string dateTimeStr) {
        if (DateTime.TryParse(dateTimeStr, out var dt)) {
            return new KintoneDateTime(dt);
        }

        throw new FormatException("Invalid datetime format.");
    }
}
