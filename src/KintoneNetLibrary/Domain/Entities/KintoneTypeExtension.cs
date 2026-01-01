namespace KintoneNetLibrary.Domain.Entities;

// コメントは日本語で記述してください
/// <summary>
/// Kintoneに関連する型の拡張メソッドを提供します。
/// </summary>
public static class KintoneTypeExtension {
    /// <summary>
    /// リストがnullまたは空であるかを判定します。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty<T>(this List<T>? list) {
        return list == null || list.Count == 0;
    }
    /// <summary>
    /// 文字列がnullまたは空であるかを判定します。
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(this string? value) {
        return string.IsNullOrEmpty(value);
    }
    /// <summary>
    /// DateTimeをKintoneの日付形式の文字列に変換します。
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ToKintoneDateString(this DateTime dateTime) {
        return dateTime.ToString("yyyy-MM-dd");
    }
    /// <summary>
    /// DateTimeをKintoneの日時形式の文字列に変換します。
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ToKintoneDateTimeString(this DateTime dateTime) {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}