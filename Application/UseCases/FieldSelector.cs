using System.Linq.Expressions;

namespace KintoneNetLibrary.Application.UseCases;

public static class FieldSelector<T> {
    /// <summary>
    /// ラムダ式からフィールド名を取得します
    /// </summary>
    public static string Select(Expression<Func<T, object>> fieldSelector) {
        if (fieldSelector == null) throw new ArgumentNullException(nameof(fieldSelector));

        // ボディがMemberExpressionまたはUnaryExpressionの場合を考慮
        Expression body = fieldSelector.Body;

        if (body is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert) {
            body = unaryExpr.Operand;
        }

        if (body is MemberExpression memberExpr) {
            return memberExpr.Member.Name;
        }

        throw new ArgumentException("フィールドセレクターはプロパティアクセス式で指定してください。");
    }
}
