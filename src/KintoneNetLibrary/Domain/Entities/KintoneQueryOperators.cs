namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// クエリ演算子定義
/// </summary>
public static class KintoneQueryOperators {
    /// <summary>
    /// 等しい
    /// </summary>
    public new const string Equals = "=";
    /// <summary>
    /// 等しくない
    /// </summary>
    public const string NotEquals = "!=";
    /// <summary>
    /// より大きい
    /// </summary>
    public const string GreaterThan = ">";
    /// <summary>
    /// より小さい
    /// </summary>
    public const string LessThan = "<";
    /// <summary>
    /// 以上
    /// </summary>
    public const string GreaterThanOrEqual = ">=";
    /// <summary>
    /// 以下
    /// </summary>
    public const string LessThanOrEqual = "<=";
    /// <summary>
    /// 〜の中に含まれる
    /// </summary>
    public const string In = "in";
    /// <summary>
    /// 〜の中に含まれない
    /// </summary>
    public const string NotIn = "not in";
    /// <summary>
    /// 〜に似ている
    /// </summary>
    public const string Like = "like";
    /// <summary>
    /// 〜に似ていない
    /// </summary>
    public const string NotLike = "not like";
}
