using System;
using System.Linq.Expressions;
using System.Text;

namespace KintoneNetLibrary;

public static class KintoneQueryExpression
{
    public static string ToQueryString<T>(Expression<Func<T, bool>> expression) {
        var visitor = new KintoneExpressionVisitor();
        visitor.Visit(expression);
        return visitor.Query;
    }

    private class KintoneExpressionVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _queryBuilder = new();

        public string Query => _queryBuilder.ToString();

        protected override Expression VisitBinary(BinaryExpression node) {
            this._queryBuilder.Append("(");
            Visit(node.Left);
            this._queryBuilder.Append($" {GetOperator(node.NodeType)} ");
            Visit(node.Right);
            this._queryBuilder.Append(")");
            return node;
        }

        protected override Expression VisitMember(MemberExpression node) {
            this._queryBuilder.Append(node.Member.Name);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node) {
            if (node.Type == typeof(string)) {
                this._queryBuilder.Append($"\"{node.Value}\"");
            } else {
                this._queryBuilder.Append(node.Value);
            }
            return node;
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
    }
}
