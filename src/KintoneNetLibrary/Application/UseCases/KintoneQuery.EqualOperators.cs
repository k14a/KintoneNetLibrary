using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    /// <summary>
    /// 等価演算子（=）を使用してフィールドの値を比較します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えられた値と等しいかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値の型がサポートされていない場合は、例外をスローします。
    /// </remarks>
    /// <typeparam name="TValue">比較する値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="value">比較する値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    public KintoneQuery<T> Equal<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue value) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ValidateSupportedType(typeof(TValue), allowStringAndBool: true);

        var parameter = fieldSelector.Parameters[0];
        var field = fieldSelector.Body;

        var constant = Expression.Constant(value, field.Type);
        var body = Expression.Equal(field, constant);

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return this.And(lambda);
    }

    /// <summary>
    /// 不等価演算子（!=）を使用してフィールドの値を比較します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えられた値と等しくないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is not null" を使用して確認します。
    /// また、値の型がサポートされていない場合は、例外をスローします。
    /// </remarks>
    /// <typeparam name="TValue">比較する値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="value">比較する値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    public KintoneQuery<T> NotEqual<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue value) {
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
