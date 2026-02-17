using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

/// <summary>
/// Kintoneのクエリ文字列を生成するためのExpressionVisitor
/// </summary>
public class KintoneExpressionVisitor : ExpressionVisitor {
    private readonly StringBuilder _queryBuilder = new();
    /// <summary>
    /// 日付時刻のタイムゾーン設定（デフォルトはローカルタイムゾーン）
    /// </summary>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>
    /// 指定された式からKintoneのクエリ文字列を生成します。
    /// </summary>
    /// <param name="expression">式</param>
    /// <returns>生成されたクエリ文字列</returns>
    public string ToQueryString(Expression expression) {
        this._queryBuilder.Clear();
        this.Visit(expression);
        return this._queryBuilder.ToString();
    }

    /// <summary>
    /// Convert 演算子を解除して中身の式を取得します。
    /// </summary>
    /// <param name="expr">式</param>
    /// <returns>変換解除後の式</returns>
    private Expression UnwrapConvert(Expression expr) {
        while (expr is UnaryExpression unary && expr.NodeType == ExpressionType.Convert) {
            expr = unary.Operand;
        }
        return expr;
    }

    /// <summary>
    /// バイナリ式を処理します。
    /// </summary>
    /// <param name="node">バイナリ式のノード</param>
    /// <returns>処理後の式</returns>
    /// <exception cref="NotSupportedException">サポートされていない式の場合にスローされます</exception>
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
            this._queryBuilder.Append(GetOperator(node.NodeType));
            this.Visit(right); // 値
        } else if (!isLeftMember && isRightMember) {
            this.Visit(rightMember); // フィールド
            this._queryBuilder.Append(GetOperator(FlipOperator(node.NodeType)));
            this.Visit(left); // 値
        } else {
            throw new NotSupportedException(
                $"フィールドが左右どちらにも見つかりません。サポートされていない式構造です。\nLeft: {left}\nRight: {right}");
        }

        return node;
    }

    /// <summary>
    /// MemberExpression をアンラップして取得します。
    /// </summary>
    /// <param name="expr">式</param>
    /// <param name="memberExpr">アンラップされたMemberExpression</param>
    /// <returns>アンラップに成功した場合はtrue、それ以外はfalse</returns>
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

    /// <summary>
    /// メンバー式を処理します。
    /// </summary>
    /// <param name="node">メンバー式のノード</param>
    /// <returns>処理後の式</returns>
    /// <exception cref="NotSupportedException">サポートされていない式の場合にスローされます</exception>
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

    /// <summary>
    /// 定数式を処理します。
    /// </summary>
    /// <param name="node">定数式のノード</param>
    /// <returns>処理後の式</returns>
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
    /// <summary>
    /// 新しい式を処理します。
    /// </summary>
    /// <param name="node">新しい式のノード</param>
    /// <returns>処理後の式</returns>
    /// <exception cref="NotSupportedException">サポートされていない式の場合にスローされます</exception>
    protected override Expression VisitNew(NewExpression node) {
        // コンストラクタ式を評価して値を取得
        var value = this.EvaluateExpression(node);

        if (value != null) {
            this._queryBuilder.Append(this.FormatValue(value));
            return node;
        }

        throw new NotSupportedException($"未対応のNew式: {node}");
    }
    /// <summary>
    /// メンバー名を取得します。
    /// </summary>
    /// <param name="node">メンバー式のノード</param>
    /// <returns>メンバー名</returns>
    private string GetMemberName(MemberExpression node) {
        if (node.Expression is MemberExpression inner) {
            return $"{this.GetMemberName(inner)}.{node.Member.Name}";
        }

        return node.Member.Name;
    }
    /// <summary>
    /// 式を評価して値を取得します。
    /// </summary>
    /// <param name="expr">評価する式</param>
    /// <returns>評価結果の値</returns>
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
    /// <summary>
    /// メソッド呼び出し式を処理します。
    /// </summary>
    /// <param name="node">メソッド呼び出し式のノード</param>
    /// <returns>処理後の式</returns>
    /// <exception cref="NotSupportedException">サポートされていない式の場合にスローされます</exception>
    protected override Expression VisitMethodCall(MethodCallExpression node) {
        // string.Contains は禁止例（そのまま例外）
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
    /// <summary>
    /// 新しい式を処理します。
    /// </summary>
    /// <param name="node">拡張式のノード</param>
    /// <returns>処理後の式</returns>
    protected override Expression VisitExtension(Expression node) {
        if (node is KintoneSpecialFieldExpression special) {
            this._queryBuilder.Append(special.FieldName);
            return node;
        }

        return base.VisitExtension(node);
    }
    /// <summary>
    /// 値をフォーマットします。
    /// </summary>
    /// <param name="value">フォーマットする値</param>
    /// <returns>フォーマット後の文字列</returns>
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

    /// <summary>
    /// MemberExpression からフィールド名を取得します。
    /// KintoneSpecialFieldExpression もサポートします。
    /// それ以外の式の場合は例外をスローします。
    /// フィールド名の取得に失敗した場合も例外をスローします。
    /// </summary>
    /// <param name="expr">評価する式</param>
    /// <returns>取得したフィールド名</returns>
    /// <exception cref="NotSupportedException">サポートされていない式の場合にスローされます</exception>
    private string GetFieldNameFromMemberExpression(Expression expr) {
        return expr switch {
            MemberExpression memberExpr => memberExpr.Member.Name,
            KintoneSpecialFieldExpression specialExpr => specialExpr.FieldName,
            _ => throw new NotSupportedException($"Unsupported field expression type: {expr.GetType().Name}"),
        };
    }
    /// <summary>
    /// 演算子に対応するKintoneクエリ文字列を取得します。
    /// </summary>
    /// <param name="nodeType">演算子の種類</param>
    /// <returns>対応するKintoneクエリ文字列</returns>
    /// <exception cref="NotSupportedException">サポートされていない演算子の場合にスローされます</exception>
    private static string GetOperator(ExpressionType nodeType) => nodeType switch {
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
    /// <summary>
    /// 演算子を反転します。
    /// </summary>
    /// <param name="op">反転する演算子</param>
    /// <returns>反転後の演算子</returns>
    private static ExpressionType FlipOperator(ExpressionType op) => op switch {
        ExpressionType.GreaterThan => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
        _ => op
    };

}

/// <summary>
/// Kintoneの特殊なフィールドを表す式
/// </summary>
/// <param name="fieldName">フィールド名</param>
public class KintoneSpecialFieldExpression(string fieldName) : Expression {
    /// <summary>
    /// フィールド名
    /// </summary>
    public string FieldName { get; } = fieldName;

    /// <summary>
    /// 式の種類を取得します。
    /// </summary>
    public override ExpressionType NodeType => ExpressionType.Extension;
    /// <summary>
    /// 式の型を取得します。
    /// </summary>
    public override Type Type => typeof(string);
}
