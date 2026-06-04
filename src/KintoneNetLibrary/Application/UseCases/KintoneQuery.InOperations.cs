using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    /// <summary>
    /// 指定フィールドの値がリストのいずれかに一致する条件を追加します。
    /// </summary>
    /// <param name="fieldSelector">フィールドセレクター</param>
    /// <param name="values">比較値のリスト</param>
    public KintoneQuery<T> In<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values) {
        return this.AddInCondition(fieldSelector, values, negate: false);
    }

    /// <summary>
    /// 指定フィールドの値がリストのいずれかに一致する条件を追加します。
    /// </summary>
    /// <param name="fieldSelector">フィールドセレクター</param>
    /// <param name="values">比較値（可変長）</param>
    public KintoneQuery<T> In<TValue>(Expression<Func<T, TValue>> fieldSelector, params TValue[] values) => this.In(fieldSelector, (IEnumerable<TValue>)values);

    /// <summary>
    /// 指定フィールドの値がリストのいずれにも一致しない条件を追加します。
    /// </summary>
    /// <param name="fieldSelector">フィールドセレクター</param>
    /// <param name="values">比較値のリスト</param>
    public KintoneQuery<T> NotIn<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values) {
        return this.AddInCondition(fieldSelector, values, negate: true);
    }

    /// <summary>
    /// 指定フィールドの値がリストのいずれにも一致しない条件を追加します。
    /// </summary>
    /// <param name="fieldSelector">フィールドセレクター</param>
    /// <param name="values">比較値（可変長）</param>
    public KintoneQuery<T> NotIn<TValue>(Expression<Func<T, TValue>> fieldSelector, params TValue[] values) {
        return this.NotIn(fieldSelector, values.AsEnumerable());
    }
    #endregion
}
