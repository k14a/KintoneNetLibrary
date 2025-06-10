using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Application.UseCases;

public class KintoneQueryExpression<T> {
    private readonly Expression<Func<T, bool>> _expression;

    public KintoneQueryExpression(Expression<Func<T, bool>> expression) {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public string ToQueryString() {
        var visitor = new KintoneExpressionVisitor();
        return visitor.ToQueryString(_expression.Body);
    }
}
