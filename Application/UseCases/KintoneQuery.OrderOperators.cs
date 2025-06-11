using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase {
    #region <<Public methods>>
    // OrderBy は最初のソート条件としてリストに追加
    public KintoneQuery<T> OrderBy(Expression<Func<T, object>> keySelector) {
        this._orderBys.Clear();
        this._orderBys.Add($"{GetFieldName(keySelector)} asc");
        return this;
    }
    // OrderByDescending は最初のソート条件としてリストに追加
    public KintoneQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) {
        this._orderBys.Clear();
        this._orderBys.Add($"{GetFieldName(keySelector)} desc");
        return this;
    }
    // ThenBy は既存のソート条件に追加
    public KintoneQuery<T> ThenBy(Expression<Func<T, object>> keySelector) {
        this._orderBys.Add($"{GetFieldName(keySelector)} asc");
        return this;
    }
    // ThenByDescending は既存のソート条件に追加
    public KintoneQuery<T> ThenByDescending(Expression<Func<T, object>> keySelector) {
        this._orderBys.Add($"{GetFieldName(keySelector)} desc");
        return this;
    }
    public KintoneQuery<T> Ascending(string field) {
        this._orderBy = $"{field} asc";
        return this;
    }
    public KintoneQuery<T> Descending(string field) {
        this._orderBy = $"{field} desc";
        return this;
    }
    #endregion

    #region <<Private method(s)>>
    #endregion
}