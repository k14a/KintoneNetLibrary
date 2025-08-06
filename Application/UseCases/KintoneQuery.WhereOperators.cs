using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public methods>>
    public KintoneQuery<T> Where(Expression<Func<T, bool>> predicate) {
        this.AddCondition(predicate);
        return this;
    }
    public KintoneQuery<T> And(Expression<Func<T, bool>> predicate) {
        this.AddCondition(predicate);
        return this;
    }
    public KintoneQuery<T> WhereIdIn(IEnumerable<string> ids) {
        if (ids == null || !ids.Any()) return this;

        var idList = ids.Select(id => (object)id).ToList();
        var parameter = Expression.Parameter(typeof(T), "x");

        // 特殊フィールド "$id" を表すカスタム式ノード
        var fieldExpr = new KintoneSpecialFieldExpression("$id");

        var containsMethod = typeof(List<object>).GetMethod("Contains", [typeof(object)]);
        var valuesExpr = Expression.Constant(idList);

        var body = Expression.Call(valuesExpr, containsMethod!, fieldExpr);
        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

        return this.Where(lambda); // この行はそのままでOK
    }
    public KintoneQuery<T> WhereIdEquals(string id) {
        this.AddEqualityCondition("$id", id);
        return this;
    }
    public KintoneQuery<T> WhereIdsEquals(IEnumerable<string> ids) {
        this.AddOrConditions("$id", ids);
        return this;
    }
    public KintoneQuery<T> WhereEquals(string field, string value) {
        this.AddEqualityCondition(field, value);
        return this;
    }
    #endregion

    #region <<Private methods>>
    #endregion
}