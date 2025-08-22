using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えられた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <param name="from">範囲の開始値</param>
    /// <param name="to">範囲の終了値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターまたは値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> Between(string field, object from, object to) =>
        this.AddBetweenCondition(field, from, to, inclusiveLower: true, inclusiveUpper: true);

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <param name="from">範囲の開始値</param>
    /// <param name="to">範囲の終了値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターまたは値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> BetweenExclusive(string field, object from, object to) =>
        this.AddBetweenCondition(field, from, to, inclusiveLower: false, inclusiveUpper: false);

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="from">範囲の開始値</param>
    /// <param name="to">範囲の終了値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
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

    /// <summary>
    /// 既存のソート条件に追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを昇順でソートするための条件を追加します。
    /// 既存のソート条件は保持され、新しい条件が最後に追加されます。
    /// </remarks>
    /// <param name="fieldSelector">ソートするフィールドを指定する式</param>
    /// <param name="value">ソートする値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
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

    /// <summary>
    /// 既存のソート条件に追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを大なりイコールでソートするための条件を追加します。
    /// 既存のソート条件は保持され、新しい条件が最後に追加されます。
    /// </remarks>
    /// <param name="fieldSelector">ソートするフィールドを指定する式</param>
    /// <param name="value">ソートする値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
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

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれるかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれるかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="value">ソートする値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
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

    /// <summary>
    /// 既存のソート条件に追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを小なりイコールでソートするための条件を追加します。
    /// 既存のソート条件は保持され、新しい条件が最後に追加されます。
    /// </remarks>
    /// <param name="fieldSelector">ソートするフィールドを指定する式</param>
    /// <param name="value">ソートする値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
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
