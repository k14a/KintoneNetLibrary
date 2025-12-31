namespace KintoneNetLibrary.Domain.Entities;

public static class KintoneTypeExtension
{
    public static bool IsNullOrEmpty<T>(this List<T>? list) {
        return list == null || list.Count == 0;
    }

    public static bool IsNullOrEmpty(this string? value) {
        return string.IsNullOrEmpty(value);
    }

    public static string ToKintoneDateString(this DateTime dateTime) {
        return dateTime.ToString("yyyy-MM-dd");
    }

    public static string ToKintoneDateTimeString(this DateTime dateTime) {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}