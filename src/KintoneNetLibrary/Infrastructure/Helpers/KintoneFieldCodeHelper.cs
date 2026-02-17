using System.Reflection;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

// コメントは日本語で記述
/// <summary>
/// Kintoneのフィールドコードに関するヘルパークラス
/// </summary>
public static class KintoneFieldCodeHelper {
    /// <summary>
    /// 指定された型からKintoneのフィールドコードを取得します。
    /// </summary>
    /// <param name="type">Kintoneのフィールドコードを取得する型</param>
    /// <returns>取得したフィールドコードの配列</returns>
    public static string[] GetKintoneFieldCodes(this Type type) {
        return type.GetProperties()
            .Select(prop => prop.GetCustomAttribute<KintoneItemAttribute>())
            .Where(attr => attr != null && !string.IsNullOrEmpty(attr.FieldCode) && attr.FieldCode.Length <= KintoneConstants.KintoneFieldCodeMaxLength)
            .Select(attr => attr.FieldCode)
            .ToArray();
    }
}
