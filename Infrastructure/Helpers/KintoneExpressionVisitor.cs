using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public class KintoneExpressionVisitor : ExpressionVisitor {
    private readonly StringBuilder _queryBuilder = new();
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    public string ToQueryString(Expression expression) {
        this._queryBuilder.Clear();
        this.Visit(expression);
        return this._queryBuilder.ToString();
    }

    // protected override Expression VisitBinary(BinaryExpression node) {
    //     // 論理演算（AND/OR）なら再帰的に処理
    //     if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse) {
    //         Visit(node.Left);
    //         _queryBuilder.Append(node.NodeType == ExpressionType.AndAlso ? " and " : " or ");
    //         Visit(node.Right);
    //         return node;
    //     }

    //     var left = node.Left;
    //     var right = node.Right;

    //     // 左右の MemberExpression を抽出
    //     bool isLeftMember = TryUnwrapMemberExpression(left, out var leftMember);
    //     bool isRightMember = TryUnwrapMemberExpression(right, out var rightMember);

    //     if (isLeftMember && !isRightMember) {
    //         Visit(leftMember); // フィールド
    //         _queryBuilder.Append(GetOperator(node.NodeType));
    //         Visit(right); // 値
    //     } else if (!isLeftMember && isRightMember) {
    //         Visit(rightMember); // フィールド
    //         _queryBuilder.Append(GetOperator(FlipOperator(node.NodeType)));
    //         Visit(left); // 値
    //     } else {
    //         throw new NotSupportedException(
    //             $"フィールドが左右どちらにも見つかりません。サポートされていない式構造です。\nLeft: {left}\nRight: {right}");
    //     }

    //     return node;
    // }
    private Expression UnwrapConvert(Expression expr) {
        while (expr is UnaryExpression unary && expr.NodeType == ExpressionType.Convert) {
            expr = unary.Operand;
        }
        return expr;
    }

    protected override Expression VisitBinary(BinaryExpression node) {
        // 論理演算（AND/OR）なら再帰的に処理
        if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse) {
            this.Visit(node.Left);
            this._queryBuilder.Append(node.NodeType == ExpressionType.AndAlso ? " and " : " or ");
            this.Visit(node.Right);
            return node;
        }

        // Convert を解除して中身を取り出す
        var left = this.UnwrapConvert(node.Left);
        var right = this.UnwrapConvert(node.Right);

        // 左右の MemberExpression を抽出
        bool isLeftMember = this.TryUnwrapMemberExpression(left, out var leftMember);
        bool isRightMember = this.TryUnwrapMemberExpression(right, out var rightMember);

        if (isLeftMember && !isRightMember) {
            this.Visit(leftMember); // フィールド
            this._queryBuilder.Append(this.GetOperator(node.NodeType));
            this.Visit(right); // 値
        } else if (!isLeftMember && isRightMember) {
            this.Visit(rightMember); // フィールド
            this._queryBuilder.Append(this.GetOperator(this.FlipOperator(node.NodeType)));
            this.Visit(left); // 値
        } else {
            throw new NotSupportedException(
                $"フィールドが左右どちらにも見つかりません。サポートされていない式構造です。\nLeft: {left}\nRight: {right}");
        }

        return node;
    }

    private bool TryUnwrapMemberExpression(Expression expr, out MemberExpression? memberExpr) {
        memberExpr = null;

        while (expr is MemberExpression me) {
            if (me.Expression is ParameterExpression) {
                memberExpr = me;
                return true;
            }

            expr = me.Expression;
        }

        return false;
    }

    // private bool IsModelMemberExpression(Expression expr) {
    //     // Unary (e.g., Convert) unwrap
    //     if (expr is UnaryExpression unary && unary.Operand is MemberExpression innerMember) {
    //         expr = innerMember;
    //     }

    //     return expr is MemberExpression memberExpr &&
    //            memberExpr.Expression is ParameterExpression;
    // }

    protected override Expression VisitMember(MemberExpression node) {
        // x.ReleaseDate! のような UnaryExpression 経由の MemberAccess に対応
        if (node.Expression is ParameterExpression) {
            var fieldName = this.GetFieldNameFromMemberExpression(node);
            this._queryBuilder.Append(fieldName);
            return node;
        }

        // 定数やクロージャ参照もサポート（値取得）
        if (node.Expression is ConstantExpression constantExpr) {
            object? container = constantExpr.Value;
            object? value = node.Member switch {
                FieldInfo fi => fi.GetValue(container),
                PropertyInfo pi => pi.GetValue(container),
                _ => null
            };

            if (value != null) {
                this._queryBuilder.Append(this.FormatValue(value));
                return node;
            }
        }

        throw new NotSupportedException($"未対応の式: {node}");
    }

    protected override Expression VisitConstant(ConstantExpression node) {
        if (node.Value == null) {
            this._queryBuilder.Append("null");
        } else if (node.Value is string str) {
            this._queryBuilder.Append($"\"{str}\"");
        } else if (node.Value is DateTime dt) {
            if (dt.Kind == DateTimeKind.Unspecified) {
                dt = TimeZoneInfo.ConvertTimeToUtc(dt, this.TimeZone);
            } else {
                dt = dt.ToUniversalTime();
            }
            this._queryBuilder.Append($"\"{dt:yyyy-MM-ddTHH:mm:ssZ}\"");
        } else if (node.Value is TimeOnly t) {
            this._queryBuilder.Append($"\"{t:HH:mm}\"");
        } else if (node.Value is bool b) {
            this._queryBuilder.Append(b.ToString().ToLower());
        } else {
            // 数値などはそのまま出力
            this._queryBuilder.Append(Convert.ToString(node.Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        return node;
    }
    protected override Expression VisitNew(NewExpression node) {
        // コンストラクタ式を評価して値を取得
        var value = this.EvaluateExpression(node);

        if (value != null) {
            this._queryBuilder.Append(this.FormatValue(value));
            return node;
        }

        throw new NotSupportedException($"未対応のNew式: {node}");
    }

    private string GetMemberName(MemberExpression node) {
        if (node.Expression is MemberExpression inner) {
            return $"{this.GetMemberName(inner)}.{node.Member.Name}";
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
        // if (node.Method.Name == nameof(string.Contains) && node.Object != null) {
        //     throw new NotSupportedException("Kintoneの仕様上、like演算子は英数字での部分一致には対応していません。");
        // }
        if (node.Method.Name == nameof(string.Contains) && node.Method.DeclaringType == typeof(string)) {
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
                try {
                    var fieldName = this.GetFieldNameFromMemberExpression(memberAccess);

                    var evaluated = this.EvaluateExpression(collection);

                    if (evaluated is IEnumerable<object> values) {
                        this._queryBuilder.Append($"{fieldName} in (");
                        this._queryBuilder.Append(string.Join(", ", values.Select(v => this.FormatValue(v))));
                        this._queryBuilder.Append(")");
                        return node;
                    }

                    if (evaluated is System.Collections.IEnumerable rawEnumerable) {
                        var formatted = rawEnumerable.Cast<object>().Select(this.FormatValue);
                        this._queryBuilder.Append($"{fieldName} in (");
                        this._queryBuilder.Append(string.Join(", ", formatted));
                        this._queryBuilder.Append(")");
                        return node;
                    }
                } catch (NotSupportedException) {
                    // フィールド名が取得できなかった → 無視して次の可能性へ
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
                    var values = this.EvaluateExpression(predicateMethodCall.Object ?? predicateMethodCall.Arguments[0]) as IEnumerable<object>;

                    if (values != null) {
                        // collectionExpr はフィールドアクセスと想定
                        if (collectionExpr is MemberExpression memberExpr) {
                            var fieldName = this.GetFieldNameFromMemberExpression(memberExpr);

                            this._queryBuilder.Append($"{fieldName} in (");
                            this._queryBuilder.Append(string.Join(", ", values.Select(v => this.FormatValue(v))));
                            this._queryBuilder.Append(")");

                            return node;
                        }
                    }
                }
            }
        }

        return base.VisitMethodCall(node);
    }
    protected override Expression VisitExtension(Expression node) {
        if (node is KintoneSpecialFieldExpression special) {
            this._queryBuilder.Append(special.FieldName);
            return node;
        }

        return base.VisitExtension(node);
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
    private string GetFieldNameFromMemberExpression(Expression expr) {
        switch (expr) {
            case MemberExpression memberExpr:
                return memberExpr.Member.Name;
            case KintoneSpecialFieldExpression specialExpr:
                return specialExpr.FieldName;
            default:
                throw new NotSupportedException($"Unsupported field expression type: {expr.GetType().Name}");
        }
    }

    private string GetOperator(ExpressionType nodeType) => nodeType switch {
        ExpressionType.Equal => " = ",
        ExpressionType.NotEqual => " != ",
        ExpressionType.GreaterThan => " > ",
        ExpressionType.GreaterThanOrEqual => " >= ",
        ExpressionType.LessThan => " < ",
        ExpressionType.LessThanOrEqual => " <= ",
        ExpressionType.AndAlso => " and ",
        ExpressionType.OrElse => " or ",
        _ => throw new NotSupportedException($"未対応の演算子: {nodeType}")
    };

    private ExpressionType FlipOperator(ExpressionType op) => op switch {
        ExpressionType.GreaterThan => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
        _ => op
    };

}

public class KintoneSpecialFieldExpression : Expression {
    public string FieldName { get; }

    public KintoneSpecialFieldExpression(string fieldName) {
        this.FieldName = fieldName;
    }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(string);
}
