using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;

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
    /// <param name="predicate">フィールドを指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> Where(Expression<Func<T, bool>> predicate) {
        this.AddCondition(predicate);
        return this;
    }

    /// <summary>
    /// 複数の条件を AND で結合します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数の条件を AND で結合します。
    /// 既存の条件は保持され、新しい条件が追加されます。
    /// </remarks>
    /// <param name="predicate">条件を指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">条件セレクターが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">条件セレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> And(Expression<Func<T, bool>> predicate) {
        this.AddCondition(predicate);
        return this;
    }

    /// <summary>
    /// 複数の条件を OR で結合します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、複数の条件を OR で結合します。
    /// 既存の条件は保持され、新しい条件が追加されます。
    /// </remarks>
    /// <param name="ids"> KintoneレコードID</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">条件セレクターが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">条件セレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    public KintoneQuery<T> WhereIdIn(IEnumerable<string> ids) {
        if (ids == null || !ids.Any()) { return this; }

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

    /// <summary>
    /// KintoneのレコードIDが指定された値と等しいかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、KintoneのレコードIDが指定された値と等しいかを確認します。
    /// KintoneのレコードIDは、"$id" フィールドを使用して参照されます。
    /// </remarks>
    /// <param name="id">KintoneのレコードID</param>
    /// <returns>KintoneQuery インスタンス</returns>
    public KintoneQuery<T> WhereIdEquals(string id) {
        this.AddEqualityCondition("$id", id);
        return this;
    }

    /// <summary>
    /// KintoneのレコードIDが指定された値のリストに含まれるかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、KintoneのレコードIDが指定された値のリストに含まれるかを確認します。
    /// KintoneのレコードIDは、"$id" フィールドを使用して参照されます。
    /// </remarks>
    /// <param name="ids">KintoneのレコードIDのリスト</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">ids が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">ids が空の場合にスローされます。</exception>
    public KintoneQuery<T> WhereIdsEquals(IEnumerable<string> ids) {
        this.AddOrConditions("$id", ids);
        return this;
    }

    /// <summary>
    /// KintoneのレコードIDが指定された値と等しいかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、KintoneのレコードIDが指定された値と等しいかを確認します。
    /// KintoneのレコードIDは、"$id" フィールドを使用して参照されます。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <param name="value">値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentException">field が null または空白の場合にスローされます。</exception>
    /// <exception cref="ArgumentNullException">value が null の場合にスローされます。</exception>
    public KintoneQuery<T> WhereEquals(string field, string value) {
        this.AddEqualityCondition(field, value);
        return this;
    }
    #endregion
    
}