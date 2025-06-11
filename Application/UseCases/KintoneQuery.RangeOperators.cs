using System.Linq.Expressions;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> {
    #region <<Public methods>>
    public KintoneQuery<T> Between(string field, object from, object to) =>
        AddBetweenCondition(field, from, to, inclusiveLower: true, inclusiveUpper: true);

    public KintoneQuery<T> BetweenExclusive(string field, object from, object to) =>
        AddBetweenCondition(field, from, to, inclusiveLower: false, inclusiveUpper: false);

    public KintoneQuery<T> NotBetween<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue from, TValue to) {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var fieldType = typeof(TValue);
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;

        ValidateSupportedType(underlyingType);

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        var fromConstant = Expression.Constant(from, fieldType);
        var toConstant = Expression.Constant(to, fieldType);

        var lt = Expression.LessThan(field, fromConstant);
        var gt = Expression.GreaterThan(field, toConstant);
        var body = Expression.OrElse(lt, gt);

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return this.And(lambda);
    }

    public KintoneQuery<T> GreaterThan<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue value) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ValidateSupportedType(typeof(TValue));

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        // value を field.Type に合わせて明示的に Constant 化
        var constant = Expression.Constant(value, field.Type);

        var body = Expression.GreaterThan(field, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

        return this.And(lambda);
    }

    public KintoneQuery<T> GreaterThanOrEqual<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue value) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ValidateSupportedType(typeof(TValue));

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        // value を field.Type に合わせて明示的に Constant 化
        var constant = Expression.Constant(value, field.Type);

        var body = Expression.GreaterThanOrEqual(field, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

        return this.And(lambda);
    }

    public KintoneQuery<T> LessThan<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue value) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ValidateSupportedType(typeof(TValue));

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        // value を field.Type に合わせて明示的に Constant 化
        var constant = Expression.Constant(value, field.Type);

        var body = Expression.LessThan(field, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

        return this.And(lambda);
    }

    public KintoneQuery<T> LessThanOrEqual<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue value) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ValidateSupportedType(typeof(TValue));

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        // value を field.Type に合わせて明示的に Constant 化
        var constant = Expression.Constant(value, field.Type);

        var body = Expression.LessThanOrEqual(field, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

        return this.And(lambda);
    }
    #endregion
}
