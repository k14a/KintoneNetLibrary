using System.Linq.Expressions;
using System.Text;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public static class KintoneQueryExpression {
    public static string ToQueryString<T>(Expression<Func<T, bool>> expression) {
        var visitor = new KintoneExpressionVisitor();
        visitor.Visit(expression);
        return visitor.Query;
    }

    private class KintoneExpressionVisitor : ExpressionVisitor {
        private readonly StringBuilder _queryBuilder = new();

        public string Query => _queryBuilder.ToString();

        protected override Expression VisitBinary(BinaryExpression node) {
            bool NeedsParentheses(Expression child, Expression parent) {
                if (child is not BinaryExpression binaryChild) return false;

                var childPrec = GetPrecedence(binaryChild.NodeType);
                var parentPrec = GetPrecedence(((BinaryExpression)parent).NodeType);

                return childPrec < parentPrec;
            }

            if (NeedsParentheses(node.Left, node)) _queryBuilder.Append("(");
            Visit(node.Left);
            if (NeedsParentheses(node.Left, node)) _queryBuilder.Append(")");

            _queryBuilder.Append($" {GetOperator(node.NodeType)} ");

            if (NeedsParentheses(node.Right, node)) _queryBuilder.Append("(");
            Visit(node.Right);
            if (NeedsParentheses(node.Right, node)) _queryBuilder.Append(")");

            return node;
        }

        protected override Expression VisitMember(MemberExpression node) {
            this._queryBuilder.Append(node.Member.Name);
            return node;
        }
        protected override Expression VisitConstant(ConstantExpression node) {
            if (node.Value == null) {
                _queryBuilder.Append("null");
                return node;
            }

            var value = node.Value;
            switch (value) {
                case string str:
                    _queryBuilder.Append($"\"{str}\"");
                    break;

                case bool b:
                    _queryBuilder.Append($"\"{b.ToString().ToLower()}\"");
                    break;

                case DateTime dt:
                    _queryBuilder.Append($"\"{dt.ToString("o")}\""); // ISO 8601
                    break;

                case KintoneDateTime kdt:
                    _queryBuilder.Append($"\"{kdt.Value.ToString("o")}\"");
                    break;

                case KintoneTimeOnly kto:
                    _queryBuilder.Append($"\"{kto.Value.ToString()}\"");
                    break;

                default:
                    _queryBuilder.Append(value);
                    break;
            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node) {
            if (node.NodeType == ExpressionType.Not) {
                // 明示的に比較されていない（例：!x.IsActive）
                if (node.Operand is MemberExpression member) {
                    _queryBuilder.Append($"{member.Member.Name} != true");
                    return node;
                }

                // 否定式（例：!(x.Approved == true)）の場合
                _queryBuilder.Append("not ");
                _queryBuilder.Append("(");
                Visit(node.Operand);
                _queryBuilder.Append(")");
                return node;
            }

            return base.VisitUnary(node);
        }
        protected override Expression VisitNewArray(NewArrayExpression node) {
            var values = node.Expressions
                .Select(expr => {
                    if (expr is ConstantExpression constExpr) {
                        return constExpr.Type == typeof(string)
                            ? $"\"{constExpr.Value}\""
                            : constExpr.Value?.ToString() ?? "";
                    }
                    return expr.ToString(); // fallback
                });

            _queryBuilder.Append("(");
            _queryBuilder.Append(string.Join(", ", values));
            _queryBuilder.Append(")");
            return node;
        }
        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (node.Method.Name == "StartsWith" && node.Object is MemberExpression memberExpr) {
                _queryBuilder.Append($"{memberExpr.Member.Name} like ");
                Visit(node.Arguments[0]);
                return node;
            }

            if (node.Method.Name is "EndsWith" or "Contains") {
                throw new NotSupportedException($"kintone クエリでは {node.Method.Name} はサポートされていません。前方一致 (StartsWith) のみ使用可能です。");
            }

            return base.VisitMethodCall(node);
        }
        private static string GetOperator(ExpressionType nodeType) {
            return nodeType switch {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "and",
                ExpressionType.OrElse => "or",
                _ => throw new NotSupportedException($"Operator '{nodeType}' is not supported")
            };
        }
        private static int GetPrecedence(ExpressionType nodeType) => nodeType switch {
            ExpressionType.OrElse => 1,
            ExpressionType.AndAlso => 2,
            ExpressionType.Equal => 3,
            ExpressionType.NotEqual => 3,
            ExpressionType.GreaterThan => 3,
            ExpressionType.GreaterThanOrEqual => 3,
            ExpressionType.LessThan => 3,
            ExpressionType.LessThanOrEqual => 3,
            _ => 99
        };

    }
}
