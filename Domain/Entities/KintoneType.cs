namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone レコードの基本タイプ定義
/// </summary>
public enum KintoneFieldType {
    Unknown,
    SingleLineText,
    Number,
    Calc,
    MultiLineText,
    RichText,
    [Obsolete("リンクタイプを明確に指定してください。LinkUrl, LinkTelephone, LinkEmail を使用してください。")]
    Link,
    LinkUrl,
    LinkTelephone,
    LinkEmail,
    CheckBox,
    RadioButton,
    DropDown,
    MultiSelect,
    File,
    Date,
    Time,
    DateTime,
    UserSelect,
    OrganizationSelect,
    GroupSelect,
    CreatedTime,
    UpdatedTime,
    CreatedBy,
    UpdatedBy,
    RecordNumber,
    Status,
    Assignee,
    Category,
    SubTable,
    Lookup
}

/// <summary>
/// クエリ演算子定義
/// </summary>
public static class KintoneQueryOperators {
    public new const string Equals = "=";
    public const string NotEquals = "!=";
    public const string GreaterThan = ">";
    public const string LessThan = "<";
    public const string GreaterThanOrEqual = ">=";
    public const string LessThanOrEqual = "<=";
    public const string In = "in";
    public const string NotIn = "not in";
    public const string Like = "like";
    public const string NotLike = "not like";
}
