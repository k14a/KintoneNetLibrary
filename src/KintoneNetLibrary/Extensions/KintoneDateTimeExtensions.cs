namespace KintoneNetLibrary.Extensions;

using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

/// <summary>
/// KintoneDateTime 拡張メソッド群
/// </summary>
public static class KintoneDateTimeExtensions {
    /// <summary>
    /// KintoneDateTime を ISO 8601 の "yyyy-MM-ddTHH:mm" 形式で文字列化（Kintone仕様に準拠）
    /// </summary>
    public static string ToKintoneString(this KintoneDateTime kdt) {
        return kdt.Value.ToString("yyyy-MM-ddTHH:mm");
    }

    /// <summary>
    /// KintoneDateTime を KintoneFieldType に応じた文字列形式で出力（Dateなら "yyyy-MM-dd"、DateTimeなら "yyyy-MM-ddTHH:mm"）
    /// </summary>
    /// <param name="kdt">変換対象のKintoneDateTime</param>
    /// <param name="type">出力するKintoneフィールドのタイプ</param>
    /// <returns>指定された形式の文字列</returns>
    /// <exception cref="NotSupportedException">サポートされていないKintoneFieldTypeの場合にスローされます</exception>
    public static string ToKintoneString(this KintoneDateTime kdt, KintoneFieldType type) {
        return type switch {
            KintoneFieldType.Date => kdt.Value.ToString("yyyy-MM-dd"),
            KintoneFieldType.DateTime => kdt.Value.ToString("yyyy-MM-ddTHH:mm"),
            _ => throw new NotSupportedException($"Unsupported DateTimeType: {type}")
        };
    }

    /// <summary>
    /// 文字列を KintoneDateTime に変換。入力は ISO 8601 の "yyyy-MM-ddTHH:mm" 形式を想定（Kintone仕様に準拠）。形式が不正な場合は FormatException をスロー。
    /// </summary>
    /// <param name="dateTimeStr">変換対象の文字列</param>
    /// <returns>変換されたKintoneDateTimeオブジェクト</returns>
    /// <exception cref="FormatException">形式が不正な場合にスローされます</exception>
    public static KintoneDateTime ToKintoneDateTime(this string dateTimeStr) {
        if (DateTime.TryParse(dateTimeStr, out var dt)) {
            return new KintoneDateTime(dt);
        }

        throw new FormatException("Invalid datetime format.");
    }
}
