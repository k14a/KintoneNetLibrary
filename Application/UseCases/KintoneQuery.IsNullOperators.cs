using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    public KintoneQuery<T> IsNull<TValue>(Expression<Func<T, TValue>> fieldSelector) {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var field = GetFieldName(fieldSelector);
        _conditions.Add($"{field} = null");

        return this;
    }
    public KintoneQuery<T> IsNull(string field) {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        // Kintoneのnull比較構文: フィールド = null
        this.AddRawCondition($"{field} = null");
        return this;
    }
    public KintoneQuery<T> IsNotNull<TValue>(Expression<Func<T, TValue>> fieldSelector) {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var field = GetFieldName(fieldSelector);
        _conditions.Add($"{field} != null");

        return this;
    }
    public KintoneQuery<T> IsNotNull(string field) {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        // Kintoneのnull比較構文: フィールド != null
        this.AddRawCondition($"{field} != null");
        return this;
    }

    #endregion
}
