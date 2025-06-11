using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase {
    #region <<Public methods>>
    public KintoneQuery<T> Equal<TValue>(
    Expression<Func<T, TValue>> fieldSelector, TValue value) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ValidateSupportedType(typeof(TValue), allowStringAndBool: true);

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        var constant = Expression.Constant(value, field.Type);
        var body = Expression.Equal(field, constant);

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return this.And(lambda);
    }

    public KintoneQuery<T> NotEqual<TValue>(
        Expression<Func<T, TValue>> fieldSelector, TValue value) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ValidateSupportedType(typeof(TValue), allowStringAndBool: true);

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        var constant = Expression.Constant(value, field.Type);
        var body = Expression.NotEqual(field, constant);

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return this.And(lambda);
    }
    #endregion
}
