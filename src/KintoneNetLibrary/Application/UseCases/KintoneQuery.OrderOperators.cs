using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    /// <summary>
    /// ソート条件を追加します。
    /// </summary>
    /// <param name="keySelector">ソートするフィールドを指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを昇順でソートするための条件を追加します。
    /// 既存のソート条件はクリアされ、新しい条件が最初のソート条件として設定されます。
    /// </remarks>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> OrderBy(Expression<Func<T, object>> keySelector) {
        this._orderBys.Clear();
        this._orderBys.Add($"{GetFieldName(keySelector)} asc");
        return this;
    }

    /// <summary>
    /// ソート条件を追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを降順でソートするための条件を追加します。
    /// 既存のソート条件はクリアされ、新しい条件が最初のソート条件として設定されます。
    /// </remarks>
    /// <param name="keySelector">ソートするフィールドを指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) {
        this._orderBys.Clear();
        this._orderBys.Add($"{GetFieldName(keySelector)} desc");
        return this;
    }

    /// <summary>
    /// 既存のソート条件に追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを昇順でソートするための条件を追加します。
    /// 既存のソート条件は保持され、新しい条件が最後に追加されます。
    /// </remarks>
    /// <param name="keySelector">ソートするフィールドを指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> ThenBy(Expression<Func<T, object>> keySelector) {
        this._orderBys.Add($"{GetFieldName(keySelector)} asc");
        return this;
    }

    /// <summary>
    /// 既存のソート条件に追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを降順でソートするための条件を追加します。
    /// 既存のソート条件は保持され、新しい条件が最後に追加されます。
    /// </remarks>
    /// <param name="keySelector">ソートするフィールドを指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> ThenByDescending(Expression<Func<T, object>> keySelector) {
        this._orderBys.Add($"{GetFieldName(keySelector)} desc");
        return this;
    }

    /// <summary>
    /// 昇順でソートします。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを昇順でソートするための条件を追加します。
    /// 既存のソート条件はクリアされ、新しい条件が最初のソート条件として設定されます。
    /// </remarks>
    /// <param name="field">ソートするフィールド名</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentException">field が null または空白の場合にスローされます。</exception>
    public KintoneQuery<T> Ascending(string field) {
        this._orderBy = $"{field} asc";
        return this;
    }

    /// <summary>
    /// 降順でソートします。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを降順でソートするための条件を追加します。
    /// 既存のソート条件はクリアされ、新しい条件が最初のソート条件として設定されます。
    /// </remarks>
    /// <param name="field">ソートするフィールド名</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentException">field が null または空白の場合にスローされます。</exception>
    public KintoneQuery<T> Descending(string field) {
        this._orderBy = $"{field} desc";
        return this;
    }
    #endregion
}