using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    public KintoneQuery<T> In<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values) {
        return this.AddInCondition(fieldSelector, values, negate: false);
    }

    public KintoneQuery<T> In<TValue>(Expression<Func<T, TValue>> fieldSelector, params TValue[] values) => this.In(fieldSelector, (IEnumerable<TValue>)values);

    public KintoneQuery<T> NotIn<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values) {
        return this.AddInCondition(fieldSelector, values, negate: true);
    }

    public KintoneQuery<T> NotIn<TValue>(Expression<Func<T, TValue>> fieldSelector, params TValue[] values) {
        return this.NotIn(fieldSelector, values.AsEnumerable());
    }
    #endregion
}
