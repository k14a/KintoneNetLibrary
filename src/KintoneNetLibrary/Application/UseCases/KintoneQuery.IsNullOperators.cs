using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    /// <summary>
    /// フィールドが null であるかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドが null であるかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値の型がサポートされていない場合は、例外をスローします。
    /// </remarks>
    /// <typeparam name="TValue">値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> IsNull<TValue>(Expression<Func<T, TValue>> fieldSelector) {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var field = GetFieldName(fieldSelector);
        this._conditions.Add($"{field} = null");

        return this;
    } 
    
    /// <summary>
    /// フィールドが null であるかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドが null であるかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値の型がサポートされていない場合は、例外をスローします。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentException">フィールド名が null または空白の場合にスローされます。</exception>
    public KintoneQuery<T> IsNull(string field) {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        // Kintoneのnull比較構文: フィールド = null
        this.AddRawCondition($"{field} = null");
        return this;
    }

    /// <summary>
    /// フィールドが null でないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドが null でないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is not null" を使用して確認します。
    /// また、値の型がサポートされていない場合は、例外をスローします。
    /// </remarks>
    /// <typeparam name="TValue">値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> IsNotNull<TValue>(Expression<Func<T, TValue>> fieldSelector) {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var field = GetFieldName(fieldSelector);
        this._conditions.Add($"{field} != null");

        return this;
    }

    /// <summary>
    /// フィールドが null でないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドが null でないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is not null" を使用して確認します。
    /// また、値の型がサポートされていない場合は、例外をスローします。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentException">フィールド名が null または空白の場合にスローされます。</exception>
    public KintoneQuery<T> IsNotNull(string field) {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        // Kintoneのnull比較構文: フィールド != null
        this.AddRawCondition($"{field} != null");
        return this;
    }
    #endregion
}
