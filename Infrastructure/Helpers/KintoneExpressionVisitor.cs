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
        // Nullable<T>.Value を特別扱い
        if (node.Member.Name == "Value" && node.Expression != null) {
            var nullableExpr = node.Expression;
            var evaluated = EvaluateExpression(nullableExpr);
            if (evaluated != null) {
                // EvaluateExpression で取り出した値（DateTime等）を VisitConstant へ渡す
                return Visit(Expression.Constant(evaluated, node.Type));
            }
        }

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
        // string.Contains は禁止例（そのまま例外）
        if (node.Method.Name == nameof(string.Contains) && node.Object != null) {
            throw new NotSupportedException("Kintoneの仕様上、like演算子は英数字での部分一致には対応していません。");
        }

        // Enumerable.Contains または IList.Contains の変換処理
        if (node.Method.Name == "Contains") {
            Expression? collection = null;
            Expression? memberAccess = null;

            if (node.Object != null && node.Arguments.Count == 1) {
                // list.Contains(x.Field)
                collection = node.Object;
                memberAccess = node.Arguments[0];
            } else if (node.Object == null && node.Arguments.Count == 2) {
                // Enumerable.Contains(list, x.Field)
                collection = node.Arguments[0];
                memberAccess = node.Arguments[1];
            }

            if (collection != null && memberAccess != null) {
                if (memberAccess is MemberExpression memberExpr) {
                    var values = EvaluateExpression(collection) as IEnumerable<object>;
                    if (values != null) {
                        var fieldName = GetFieldNameFromMemberExpression(memberExpr);
                        _queryBuilder.Append($"{fieldName} in (");
                        _queryBuilder.Append(string.Join(", ", values.Select(v => FormatValue(v))));
                        _queryBuilder.Append(")");
                        return node;
                    }
                }
            }
        }

        // 新規追加：Anyの変換処理
        if (node.Method.Name == "Any" && node.Arguments.Count == 2) {
            // 例: x.MultiSelector.Any(s => list.Contains(s))
            // node.Object は null (staticメソッド呼び出し)
            // node.Arguments[0]: 対象のコレクション (x.MultiSelector)
            // node.Arguments[1]: ラムダ式 (s => list.Contains(s))

            var collectionExpr = node.Arguments[0];
            var predicate = node.Arguments[1] as LambdaExpression;

            if (predicate != null && predicate.Body is MethodCallExpression predicateMethodCall) {
                // predicate の中身が list.Contains(s) か確認
                if (predicateMethodCall.Method.Name == "Contains") {
                    // list.Contains(s) の list 部分を評価して値を取得
                    var values = EvaluateExpression(predicateMethodCall.Object ?? predicateMethodCall.Arguments[0]) as IEnumerable<object>;

                    if (values != null) {
                        // collectionExpr はフィールドアクセスと想定
                        if (collectionExpr is MemberExpression memberExpr) {
                            var fieldName = GetFieldNameFromMemberExpression(memberExpr);

                            _queryBuilder.Append($"{fieldName} in (");
                            _queryBuilder.Append(string.Join(", ", values.Select(v => FormatValue(v))));
                            _queryBuilder.Append(")");

                            return node;
                        }
                    }
                }
            }
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
    private static string GetFieldNameFromMemberExpression(MemberExpression memberExpr) {
        // ネストされた場合は再帰的に親の名前も取得（例: b.ReleaseDate.Value など）
        if (memberExpr.Expression is MemberExpression innerMember) {
            return GetFieldNameFromMemberExpression(innerMember) + "." + memberExpr.Member.Name;
        }

        return memberExpr.Member.Name;
    }

}
