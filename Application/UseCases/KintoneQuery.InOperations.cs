using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    /// <summary>
    /// フィールドの値が指定された値のリストに含まれるかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えられた値のリストに含まれるかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <typeparam name="TValue">値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="values">値のリスト</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターまたは値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> In<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values) {
        return this.AddInCondition(fieldSelector, values, negate: false);
    }

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれるかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれるかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <typeparam name="TValue">値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="values">値のリスト</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターまたは値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> In<TValue>(Expression<Func<T, TValue>> fieldSelector, params TValue[] values) => this.In(fieldSelector, (IEnumerable<TValue>)values);

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えられた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <typeparam name="TValue">値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="values">値のリスト</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターまたは値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> NotIn<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values) {
        return this.AddInCondition(fieldSelector, values, negate: true);
    }
    
    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <typeparam name="TValue">値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="values">値のリスト</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターまたは値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> NotIn<TValue>(Expression<Func<T, TValue>> fieldSelector, params TValue[] values) {
        return this.NotIn(fieldSelector, values.AsEnumerable());
    }
    #endregion
}
