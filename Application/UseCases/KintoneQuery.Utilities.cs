using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public method(s)>>
    // ソート条件の取得メソッド（ToStringやBuild内で使用）
    public KintoneQuery<T> Limit(int limit) {
        this._limit = limit;
        return this;
    }
    [Obsolete("'offset' は使用できません。カーソルAPIを利用してください。")]
    public KintoneQuery<T> Offset(int offset) {
        throw new KintoneException("'offset' は使用できません。カーソルAPIを利用してください。");
    }
    public KintoneQuery<T> SetQuery(string query) {
        this._conditions.Clear();
        if (!string.IsNullOrEmpty(query)) {
            this._conditions.Add(query);
        }
        return this;
    }

    #endregion

    #region <<Private method(s)>>
    private void AddCondition(Expression<Func<T, bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);
        var condition = new KintoneQueryExpression<T>(predicate) { TimeZone = this.TimeZone }.ToQueryString();
        _conditions.Add(condition);
    }
    private void AddRawCondition(string condition) {
        if (!string.IsNullOrWhiteSpace(condition)) {
            _conditions.Add(condition);
        }
    }
    private void AddEqualityCondition(string field, string value) {
        if (!string.IsNullOrWhiteSpace(field)) {
            AddRawCondition($"{field}=\"{value}\"");
        }
    }
    private void AddOrConditions(string field, IEnumerable<string> values) {
        if (values != null && values.Any()) {
            var conditions = string.Join(" or ", values.Select(v => $"{field}=\"{v}\""));
            AddRawCondition(conditions);
        }
    }
    private string BuildOrderBy() => this._orderBys.Count == 0 ? string.Empty : "order by " + string.Join(", ", _orderBys);
    // ベースの基本的な型群
    private static readonly HashSet<Type> BaseSupportedTypes = new HashSet<Type> {
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal),
        typeof(DateTime), typeof(DateTimeOffset)
    };
    // 比較用セットはベースをコピーするだけ
    private static readonly HashSet<Type> SupportedTypesForComparison = new HashSet<Type>(BaseSupportedTypes);
    // 等価比較用セットはベースに文字列とboolを追加
    private static readonly HashSet<Type> SupportedTypesForEquality = new HashSet<Type>(BaseSupportedTypes) {
        typeof(string),
        typeof(bool)
    };

    private void ValidateSupportedType(Type type, bool allowStringAndBool = false) {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (allowStringAndBool) {
            if (!SupportedTypesForEquality.Contains(underlyingType)) {
                throw new NotSupportedException(
                    $"この操作は数値型や日付型、文字列、真偽値フィールドでのみ使用可能です（現在の型: {type.Name}）");
            }
        } else {
            if (!SupportedTypesForComparison.Contains(underlyingType)) {
                throw new NotSupportedException(
                    $"この操作は数値型や日付型フィールドでのみ使用可能です（現在の型: {type.Name}）");
            }
        }
    }
    private KintoneQuery<T> AddBetweenCondition(string field, object from, object to, bool inclusiveLower, bool inclusiveUpper) {
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Field name must be specified.", nameof(field));

        if (from == null || to == null)
            throw new ArgumentNullException("from/to cannot be null");

        var valueType = from.GetType();
        if (valueType != to.GetType())
            throw new ArgumentException($"from（{valueType.Name}）と to（{to.GetType().Name}）の型は一致している必要があります。");

        // 型のサポート確認
        ValidateSupportedType(valueType, allowStringAndBool: false);

        // 値の比較
        if (Comparer<object>.Default.Compare(from, to) > 0)
            throw new ArgumentException("from must be less than or equal to to");

        // 演算子選択
        var lowerOp = inclusiveLower ? ">=" : ">";
        var upperOp = inclusiveUpper ? "<=" : "<";

        // フォーマットして条件追加
        var fromStr = FormatValue(from);
        var toStr = FormatValue(to);

        _conditions.Add($"{field} {lowerOp} {fromStr} and {field} {upperOp} {toStr}");
        return this;
    }
    private string FormatValue(object value) {
        return value switch {
            string s => $"\"{s}\"",
            DateTime dt => $"\"{dt:yyyy-MM-ddTHH:mm:ssZ}\"",
            DateOnly d => $"\"{d:yyyy-MM-dd}\"",
            TimeOnly t => $"\"{t:HH:mm}\"",
            bool b => b.ToString().ToLowerInvariant(),
            int or long or float or double or decimal => value.ToString()!,  // 数値はそのまま
            _ => $"\"{value?.ToString() ?? string.Empty}\""  // それ以外は文字列として引用符付き
        };
    }
    private KintoneQuery<T> AddInCondition<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values, bool negate) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ArgumentNullException.ThrowIfNull(values);

        var valueList = values.ToList();
        if (valueList.Count == 0)
            throw new ArgumentException("値のリストが空です。", nameof(values));

        var type = typeof(TValue);
        ValidateSupportedType(type, allowStringAndBool: true);  // Equal系と同じチェック

        var memberExpr = fieldSelector.Body as MemberExpression;
        if (memberExpr == null)
            throw new NotSupportedException("フィールドセレクタは MemberExpression である必要があります");

        var fieldName = memberExpr.Member.Name;

        var formattedValues = valueList.Select(v => FormatValue(v));
        var joinedValues = string.Join(", ", formattedValues);
        var operatorStr = negate ? "not in" : "in";

        _conditions.Add($"{fieldName} {operatorStr} ({joinedValues})");
        return this;
    }
    private static string GetFieldName<TValue>(Expression<Func<T, TValue>> keySelector) {
        ArgumentNullException.ThrowIfNull(keySelector);

        MemberExpression? memberExpr = keySelector.Body switch {
            MemberExpression m => m,
            UnaryExpression u when u.NodeType == ExpressionType.Convert => u.Operand as MemberExpression,
            _ => null
        };

        if (memberExpr == null) {
            throw new ArgumentException("無効なフィールド指定です。", nameof(keySelector));
        }

        return memberExpr.Member.Name;
    }
    #endregion
}
