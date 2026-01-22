using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Converters;

/// <summary>
/// kintone のフィールドタイプ文字列を KintoneFieldType に変換するマッパー。
/// </summary>
public static class KintoneFieldTypeMapper {
    /// <summary>
    /// kintone 側で値を持たないフィールドタイプ（スキップ対象）
    /// </summary>
    private static readonly HashSet<string> SkipTypes =
        new(StringComparer.OrdinalIgnoreCase) {
            "GROUP",
            "SPACER",
            "HR"
        };

    /// <summary>
    /// kintone の type → KintoneFieldType のマッピング表
    /// </summary>
    private static readonly Dictionary<string, KintoneFieldType> TypeMap =
        new(StringComparer.OrdinalIgnoreCase) {
            ["SINGLE_LINE_TEXT"] = KintoneFieldType.SingleLineText,
            ["MULTI_LINE_TEXT"] = KintoneFieldType.MultiLineText,
            ["NUMBER"] = KintoneFieldType.Number,
            ["CALC"] = KintoneFieldType.Calc,
            ["DROP_DOWN"] = KintoneFieldType.DropDown,
            ["CHECK_BOX"] = KintoneFieldType.CheckBox,
            ["RADIO_BUTTON"] = KintoneFieldType.RadioButton,
            ["DATE"] = KintoneFieldType.Date,
            ["TIME"] = KintoneFieldType.Time,
            ["DATETIME"] = KintoneFieldType.DateTime,
            ["LINK"] = KintoneFieldType.Link,
            ["FILE"] = KintoneFieldType.File,
            ["USER_SELECT"] = KintoneFieldType.UserSelect,
            ["GROUP_SELECT"] = KintoneFieldType.GroupSelect,
            ["ORGANIZATION_SELECT"] = KintoneFieldType.OrganizationSelect,
            ["RICH_TEXT"] = KintoneFieldType.RichText,
            ["LOOKUP"] = KintoneFieldType.Lookup,
            ["STATUS"] = KintoneFieldType.Status,
            ["STATUS_ASSIGNEE"] = KintoneFieldType.StatusAssignee,
            ["CATEGORY"] = KintoneFieldType.Category,
            ["MULTI_SELECT"] = KintoneFieldType.MultiSelect,
            ["REVISION"] = KintoneFieldType.Revision,
            ["CREATOR"] = KintoneFieldType.Creator,
            ["MODIFIER"] = KintoneFieldType.Modifier,
            ["UPDATED_TIME"] = KintoneFieldType.UpdatedTime,
            ["CREATED_TIME"] = KintoneFieldType.CreatedTime,
            ["LINK_URL"] = KintoneFieldType.LinkUrl,
            ["LINK_TELEPHONE"] = KintoneFieldType.LinkTelephone,
            ["LINK_EMAIL"] = KintoneFieldType.LinkEmail,
            ["SUBTABLE"] = KintoneFieldType.SubTable
        };

    /// <summary>
    /// kintone の type を KintoneFieldType に変換します。
    /// </summary>
    /// <param name="type">kintone のフィールドタイプ文字列</param>
    /// <param name="fieldType">変換後の KintoneFieldType</param>
    /// <returns>変換できた場合 true、スキップまたは未知タイプの場合 false</returns>
    public static bool TryConvert(string type, out KintoneFieldType fieldType) {
        // スキップ対象
        if (SkipTypes.Contains(type)) {
            fieldType = default;
            return false;
        }

        // マッピング表に存在するか？
        if (TypeMap.TryGetValue(type, out fieldType)) {
            return true;
        }

        // 未知タイプ → スキップ扱い
        fieldType = default;
        return false;
    }

    /// <summary>
    /// スキップ対象かどうか判定します。
    /// </summary>
    public static bool IsSkipped(string type) => SkipTypes.Contains(type);
}
