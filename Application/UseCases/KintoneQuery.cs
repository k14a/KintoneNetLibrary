using System.Linq.Expressions;
using System.Text;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public class KintoneQuery<T> where T : KintoneModelBase {
    private readonly List<string> _conditions = [];
    private string? _orderBy;
    private int? _limit;
    private int? _offset;

    public KintoneQuery() { }
    public KintoneQuery(Expression<Func<T, bool>> predicate) {
        var expression = new KintoneQueryExpression<T>(predicate);
        this._conditions.Add(expression.ToQueryString());
    }

    public KintoneQuery<T> Where(Expression<Func<T, bool>> predicate) {
        var expression = new KintoneQueryExpression<T>(predicate);
        this._conditions.Add(expression.ToQueryString());
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

    public KintoneQuery<T> Limit(int limit) {
        this._limit = limit;
        return this;
    }

    [Obsolete("'offset' は使用できません。カーソルAPIを利用してください。")]
    public KintoneQuery<T> Offset(int offset) {
        throw new KintoneException("'offset' は使用できません。カーソルAPIを利用してください。");
        // this._offset = offset;
        // return this;
    }

    public KintoneQuery<T> WhereIdEquals(string id) {
        if (string.IsNullOrWhiteSpace(id)) { return this; }

        this._conditions.Add($"$id = \"{id}\"");
        return this;
    }

    public KintoneQuery<T> WhereIdsEquals(IEnumerable<string> ids) {
        if (ids == null || !ids.Any()) { return this; }

        this._conditions.Add(string.Join(" or ", ids.Select(x => $"$id=\"{x}\"")));
        return this;
    }

    /// <summary>
    /// IDフィールドが指定したIDリストのどれかに一致する条件を追加します。
    /// </summary>
    public KintoneQuery<T> WhereIdIn(IList<string> ids) {
        if (ids == null || ids.Count == 0) return this;

        var quotedIds = ids.Select(id => $"\"{id}\"");
        this._conditions.Add($"$id in ({string.Join(", ", quotedIds)})");
        return this;
    }

    /// <summary>
    /// 指定したフィールドが指定した値に等しい条件を追加します。
    /// </summary>
    public KintoneQuery<T> WhereEquals(string field, string value) {
        if (string.IsNullOrEmpty(field)) return this;

        this._conditions.Add($"{field} = \"{value}\"");
        return this;
    }

    /// <summary>
    /// 任意のクエリ文字列を直接設定します。既存条件はクリアされます。
    /// </summary>
    public KintoneQuery<T> SetQuery(string query) {
        this._conditions.Clear();
        if (!string.IsNullOrEmpty(query)) {
            this._conditions.Add(query);
        }
        return this;
    }

    /// <summary>
    /// 条件を "and" でつなげたクエリ文字列を返します。
    /// </summary>
    /// <param name="urlEncode">URLエンコードするかどうか</param>
    public string Build() {
        var query = string.Join(" and ", _conditions);
        if (query.Contains("offset", StringComparison.OrdinalIgnoreCase)) {
            throw new KintoneException("'offset' は使用できません。カーソルAPIを利用してください。");
        }
        return query;
    }

    private readonly List<string> _orderBys = new();

    // OrderBy は最初のソート条件としてリストに追加
    public KintoneQuery<T> OrderBy(Expression<Func<T, object>> keySelector) {
        _orderBys.Clear();
        _orderBys.Add($"{GetFieldName(keySelector)} asc");
        return this;
    }

    // OrderByDescending は最初のソート条件としてリストに追加
    public KintoneQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) {
        _orderBys.Clear();
        _orderBys.Add($"{GetFieldName(keySelector)} desc");
        return this;
    }

    // ThenBy は既存のソート条件に追加
    public KintoneQuery<T> ThenBy(Expression<Func<T, object>> keySelector) {
        _orderBys.Add($"{GetFieldName(keySelector)} asc");
        return this;
    }

    // ThenByDescending は既存のソート条件に追加
    public KintoneQuery<T> ThenByDescending(Expression<Func<T, object>> keySelector) {
        _orderBys.Add($"{GetFieldName(keySelector)} desc");
        return this;
    }

    // ソート条件の取得メソッド（ToStringやBuild内で使用）
    private string BuildOrderBy() {
        if (_orderBys.Count == 0) return string.Empty;
        return " order by " + string.Join(", ", _orderBys);
    }

    public string ToQueryString() => this.ToString();
    // public override string ToString() {
    //     var query = new StringBuilder();

    //     if (this._conditions.Count != 0) {
    //         query.Append(string.Join(" and ", this._conditions));
    //     }

    //     var orderByClause = BuildOrderBy();
    //     if (!string.IsNullOrEmpty(orderByClause)) {
    //         query.Append(orderByClause);
    //     }

    //     if (_limit.HasValue) {
    //         query.Append($" limit {_limit.Value}");
    //     }

    //     if (_offset.HasValue) {
    //         query.Append($" offset {_offset.Value}");
    //     }

    //     return query.ToString();
    // }
    public override string ToString() {
        var query = new StringBuilder();

        if (this._conditions.Count != 0) {
            query.Append(string.Join(" and ", this._conditions));
        }

        var orderByClause = BuildOrderBy();
        if (!string.IsNullOrEmpty(orderByClause)) {
            query.Append(orderByClause);
        }

        if (_limit.HasValue) {
            query.Append($" limit {_limit.Value}");
        }

        if (_offset.HasValue) {
            query.Append($" offset {_offset.Value}");
        }

        return query.ToString();
    }

    private static string GetFieldName(Expression<Func<T, object>> keySelector) {
        ArgumentNullException.ThrowIfNull(keySelector);

        MemberExpression? memberExpr = null;

        if (keySelector.Body is MemberExpression m) {
            memberExpr = m;
        } else if (keySelector.Body is UnaryExpression u && u.NodeType == ExpressionType.Convert) {
            memberExpr = u.Operand as MemberExpression;
        }

        if (memberExpr == null) {
            throw new ArgumentException("無効なフィールド指定です。", nameof(keySelector));
        }

        return memberExpr.Member.Name;
    }

}

