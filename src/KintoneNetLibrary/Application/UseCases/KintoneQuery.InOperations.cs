using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    public KintoneQuery<T> In<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values) {
        return this.AddInCondition(fieldSelector, values, negate: false);
    }

    /// <summary>
    /// 指定したフィールドが、与えられた値のいずれかと等しいという条件をクエリに追加します。
    /// 例: .In(r => r.Status, "Open", "In Progress") は、Status フィールドが "Open" または "In Progress" のレコードを対象とします。
    /// </summary>
    /// <param name="fieldName">条件を適用するフィールドの名前</param>
    /// <param name="values">フィールドが一致する値のコレクション</param>
    /// <returns>更新されたKintoneQueryオブジェクト</returns>
    /// <exception cref="ArgumentException"></exception>
    public KintoneQuery<T> In(string fieldName, IEnumerable<string> values) {
        if (string.IsNullOrWhiteSpace(fieldName)) {
            throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
        }

        var valueList = values.ToList();
        if (valueList.Count == 0) {
            throw new ArgumentException("Values collection cannot be empty", nameof(values));
        }

        var formattedValues = valueList.Select(v => FormatValue(v));
        var joinedValues = string.Join(", ", formattedValues);
        var condition = $"{fieldName} in ({joinedValues})";
        return this;
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
