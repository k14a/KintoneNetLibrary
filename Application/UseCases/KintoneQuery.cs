using System.Linq.Expressions;
using System.Text;

namespace KintoneNetLibrary.Application.UseCases;

public class KintoneQuery<T> {
    private readonly List<string> _conditions = [];
    private string? _orderBy;
    private int? _limit;
    private int? _offset;

    public KintoneQuery<T> Where(Expression<Func<T, bool>> predicate) {
        var condition = KintoneQueryExpressionParser.Parse(predicate);
        this._conditions.Add(condition);
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

    public KintoneQuery<T> Offset(int offset) {
        this._offset = offset;
        return this;
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
    public string Build(bool urlEncode = false) {
        var queryStr = string.Join(" and ", _conditions);
        if (urlEncode) {
            return Uri.EscapeDataString(queryStr);
        } else {
            return queryStr;
        }
    }

    public override string ToString() {
        var query = new StringBuilder();

        if (this._conditions.Any()) {
            query.Append(string.Join(" and ", this._conditions));
        }

        if (!string.IsNullOrEmpty(_orderBy)) {
            query.Append($" order by {this._orderBy}");
        }

        if (_limit.HasValue) {
            query.Append($" limit {this._limit.Value}");
        }

        if (_offset.HasValue) {
            query.Append($" offset {this._offset.Value}");
        }

        return query.ToString();
    }
}

internal static class KintoneQueryExpressionParser {
    public static string Parse<T>(Expression<Func<T, bool>> expression) {
        // このメソッドは、Expression を解析して kintone のクエリ文字列を生成するロジックを実装します。
        // 実装は省略していますが、必要に応じて追加してください。
        throw new NotImplementedException("Expression の解析ロジックを実装してください。");
    }
}
