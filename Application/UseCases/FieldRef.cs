using System.Linq.Expressions;

namespace KintoneNetLibrary.Application.UseCases;

/// <summary>
/// フィールド名を型安全に取得するためのユーティリティクラス
/// </summary>
/// <typeparam name="T">モデル型</typeparam>
public class FieldRef<T> {
    /// <summary>
    /// フィールド名
    /// </summary>
    /// <remarks>このフィールド名は、Kintoneのフィールドコードとして使用されます。</remarks>
    public string FieldName { get; }

    /// <summary>
    /// FieldRef のコンストラクタ
    /// </summary>
    /// <remarks>フィールド名は、Kintoneのフィールドコードとして使用されます。</remarks>
    /// <param name="fieldName">フィールド名</param>
    /// <exception cref="ArgumentNullException">フィールド名が null の場合にスローされます。</exception>
    private FieldRef(string fieldName) { this.FieldName = fieldName; }

    /// <summary>
    /// フィールド名を指定して FieldRef を作成します。
    /// </summary>
    /// <remarks>フィールド名は、Kintoneのフィールドコードとして使用されます。</remarks>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <returns>FieldRef インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public static FieldRef<T> Create(Expression<Func<T, object>> fieldSelector) {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var memberExpr = ExtractMemberExpression(fieldSelector.Body) ?? throw new ArgumentException("フィールドを指定してください。", nameof(fieldSelector));
        return new FieldRef<T>(memberExpr.Member.Name);
    }

    /// <summary>
    /// フィールド名を指定して FieldRef を作成します。
    /// </summary>
    /// <remarks>フィールド名は、Kintoneのフィールドコードとして使用されます。</remarks>
    /// <param name="expr"></param>
    /// <returns>FieldRef インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールド名が null の場合にスローされます。</exception>
    private static MemberExpression? ExtractMemberExpression(Expression expr) {
        if (expr is MemberExpression memberExpr) {
            return memberExpr;
        }

        // ボックス化などでUnaryExpressionの場合はオペランドを取得
        if (expr is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert) {
            return unaryExpr.Operand as MemberExpression;
        }

        return null;
    }

    /// <summary>
    /// フィールド名を文字列として返します。
    /// </summary>
    /// <returns>フィールド名</returns>
    public override string ToString() => this.FieldName;
}
