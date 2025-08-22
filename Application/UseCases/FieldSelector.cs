using System.Linq.Expressions;

namespace KintoneNetLibrary.Application.UseCases;

// <summary>
// フィールド名を型安全に取得するためのユーティリティクラス
// </summary>
// <typeparam name="T">モデル型</typeparam>
[Obsolete("未使用のため廃止予定")]
public static class FieldSelector<T> {
    /// <summary>
    /// ラムダ式からフィールド名を取得します
    /// </summary>
    public static string Select(Expression<Func<T, object>> fieldSelector) {
        ArgumentNullException.ThrowIfNull(fieldSelector);

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
