using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public class KintoneExpressionVisitor : ExpressionVisitor {
    private readonly StringBuilder _queryBuilder = new();

    public string ToQueryString(Expression expression) {
        _queryBuilder.Clear();
        Visit(expression);
        return _queryBuilder.ToString();
    }

    protected override Expression VisitBinary(BinaryExpression node) {
        Visit(node.Left);

        string op = node.NodeType switch {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " != ",
            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",
            ExpressionType.AndAlso => " and ",
            ExpressionType.OrElse => " or ",
            _ => throw new NotSupportedException($"Unsupported binary operator: {node.NodeType}")
        };

        _queryBuilder.Append(op);

        // EvaluateExpressionではなく、Visitで処理を任せるようにする
        Visit(node.Right);

        return node;
    }

    protected override Expression VisitMember(MemberExpression node) {
        // 左辺: モデルのプロパティへのアクセス（例: b.ReleaseDate.Value）
        Expression? current = node;
        while (current is MemberExpression memberExpr) {
            if (memberExpr.Expression is ParameterExpression) {
                // 最も外側のプロパティを取得
                var outerMember = memberExpr.Member;
                var kintoneAttr = outerMember.GetCustomAttributes(typeof(KintoneItemAttribute), true)
                    .FirstOrDefault() as KintoneItemAttribute;

                var fieldCode = kintoneAttr?.FieldCode ?? outerMember.Name;
                _queryBuilder.Append(fieldCode);
                return node;
            }

            current = memberExpr.Expression;
        }

        // 右辺: クロージャ変数等の静的評価
        if (node.Expression is ConstantExpression constantExpr) {
            object? container = constantExpr.Value;
            if (container != null) {
                object? value = node.Member switch {
                    FieldInfo fi => fi.GetValue(container),
                    PropertyInfo pi => pi.GetValue(container),
                    _ => null
                };

                return Visit(Expression.Constant(value, node.Type));
            }
        }

        _queryBuilder.Append("null");
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node) {
        if (node.Value == null) {
            _queryBuilder.Append("null");
        } else if (node.Value is string str) {
            _queryBuilder.Append($"\"{str}\"");
        } else if (node.Value is DateTime dt) {
            _queryBuilder.Append($"\"{dt:yyyy-MM-ddTHH:mm:ssZ}\"");
        } else if (node.Value is TimeOnly t) {
            _queryBuilder.Append($"\"{t:HH:mm}\"");
        } else if (node.Value is bool b) {
            _queryBuilder.Append(b.ToString().ToLower());
        } else {
            // 数値などはそのまま出力
            _queryBuilder.Append(Convert.ToString(node.Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        return node;
    }

    private string GetMemberName(MemberExpression node) {
        if (node.Expression is MemberExpression inner) {
            return $"{GetMemberName(inner)}.{node.Member.Name}";
        }

        return node.Member.Name;
    }

    private object? EvaluateExpression(Expression expr) {
        try {
            var lambda = Expression.Lambda(expr);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        } catch (Exception ex) {
            Console.WriteLine($"Expression evaluation failed: {ex.Message}");
            return null;
        }
    }

    protected override Expression VisitMethodCall(MethodCallExpression node) {
        if (node.Method.Name == nameof(string.Contains) && node.Object != null) {
            throw new NotSupportedException("Kintoneの仕様上、like演算子は英数字での部分一致には対応していません。");
        }

        return base.VisitMethodCall(node);
    }

    private string FormatValue(object? value) {
        return value switch {
            null => "null",
            TimeOnly t => $"\"{t:HH:mm}\"", // ← 追加
            string s => $"\"{s}\"",
            bool b => b.ToString().ToLower(),
            DateTime dt => $"\"{dt.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}\"",
            KintoneDateTime kdt => $"\"{kdt.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}\"",
            KintoneTimeOnly kto => $"\"{kto.Value}\"",
            int or long or float or double or decimal => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
            _ => $"\"{value?.ToString() ?? "null"}\""
        };
    }

}
