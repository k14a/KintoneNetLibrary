using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Application.UseCases;

public class KintoneQueryExpression<T> {
    public Expression<Func<T, bool>> Predicate { get; set; }
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    // public KintoneQueryExpression() {  }

    public KintoneQueryExpression(Expression<Func<T, bool>> expression) {
        Predicate = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public string ToQueryString() {
        if (this.Predicate == null) { return string.Empty; }
        var visitor = new KintoneExpressionVisitor { TimeZone = this.TimeZone };
        return visitor.ToQueryString(Predicate.Body);
    }
}
