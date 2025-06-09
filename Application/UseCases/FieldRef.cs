using System.Linq.Expressions;

namespace KintoneNetLibrary.Application.UseCases;

/// <summary>
/// フィールド名を型安全に取得するためのユーティリティクラス
/// </summary>
/// <typeparam name="T">モデル型</typeparam>
public class FieldRef<T> {
    public string FieldName { get; }

    private FieldRef(string fieldName) {
        FieldName = fieldName;
    }

    /// <summary>
    /// ラムダ式からフィールド名を取得し、FieldRef を生成します。
    /// 例: FieldRef<BookModel>.Create(x => x.Title) → "Title"
    /// </summary>
    /// <param name="fieldSelector">フィールドを指定するラムダ式</param>
    public static FieldRef<T> Create(Expression<Func<T, object>> fieldSelector) {
        if (fieldSelector == null) throw new ArgumentNullException(nameof(fieldSelector));

        var memberExpr = ExtractMemberExpression(fieldSelector.Body);
        if (memberExpr == null) {
            throw new ArgumentException("フィールドを指定してください。", nameof(fieldSelector));
        }

        return new FieldRef<T>(memberExpr.Member.Name);
    }

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

    public override string ToString() => FieldName;
}
