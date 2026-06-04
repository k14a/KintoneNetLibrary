using System.Linq.Expressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Public method(s)>>
    /// <summary>
    /// クエリの結果の最大件数を設定します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、クエリの結果の最大件数を設定します。
    /// Kintone のクエリでは、最大件数は 1000 件まで設定できます。
    /// </remarks>
    /// <param name="limit">取得する最大件数</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentOutOfRangeException">limit が 1 未満または 1000 を超える場合にスローされます。</exception>
    /// <exception cref="KintoneException">Kintone の制限により、limit が 1000 を超える場合にスローされます。</exception>
    public KintoneQuery<T> Limit(int limit) {
        this._limit = limit;
        return this;
    }

    /// <summary>
    /// クエリの結果のオフセットを設定します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、クエリの結果のオフセットを設定します。
    /// Kintone のクエリでは、オフセットはサポートされていません。
    /// 代わりにカーソルAPIを使用してください。
    /// </remarks>
    /// <param name="offset">オフセット値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="KintoneException">'offset' は使用できません。カーソルAPIを利用してください。</exception>
    [Obsolete("'offset' は使用できません。カーソルAPIを利用してください。")]
    public KintoneQuery<T> Offset(int offset) {
        throw new KintoneException("'offset' は使用できません。カーソルAPIを利用してください。");
    }

    /// <summary>
    /// クエリの条件を設定します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、クエリの条件を設定します。
    /// Kintone のクエリでは、条件は文字列で指定されます。
    /// </remarks>
    /// <param name="query">クエリの条件文字列</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">query が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">query が空文字列の場合にスローされます。</exception>
    public KintoneQuery<T> SetQuery(string query) {
        this._conditions.Clear();
        if (!string.IsNullOrEmpty(query)) {
            this._conditions.Add(query);
        }
        return this;
    }
    #endregion

    #region <<Private method(s)>>
    /// <summary>
    /// クエリの条件を追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、クエリの条件を追加します。
    /// Kintone のクエリでは、条件は文字列で指定されます。
    /// </remarks>
    /// <param name="predicate">クエリの条件を指定する式</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">predicate が null の場合にスローされます。</exception>
    private void AddCondition(Expression<Func<T, bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);
        var condition = new KintoneQueryExpression<T>(predicate) { TimeZone = this.TimeZone }.ToQueryString();
        this._conditions.Add(condition);
    }

    /// <summary>
    /// クエリの条件を追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、クエリの条件を追加します。
    /// Kintone のクエリでは、条件は文字列で指定されます。
    /// </remarks>
    /// <param name="condition">クエリの条件文字列</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">condition が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">condition が空文字列の場合にスローされます。</exception>
    private void AddRawCondition(string condition) {
        if (!string.IsNullOrWhiteSpace(condition)) {
            this._conditions.Add(condition);
        }
    }

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれるかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えられた値のリストに含まれるかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <param name="value">値</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールド名または値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールド名が空文字列の場合にスローされます。</exception>
    private void AddEqualityCondition(string field, string value) {
        if (!string.IsNullOrWhiteSpace(field)) {
            this.AddRawCondition($"{field}=\"{value}\"");
        }
    }

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <param name="values">値のリスト</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールド名または値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールド名が空文字列の場合にスローされます。</exception>
    private void AddOrConditions(string field, IEnumerable<string> values) {
        if (values != null && values.Any()) {
            var conditions = string.Join(" or ", values.Select(v => $"{field}=\"{v}\""));
            this.AddRawCondition(conditions);
        }
    }

    /// <summary>
    /// 既存のソート条件に追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを昇順でソートするための条件を追加します。
    /// 既存のソート条件は保持され、新しい条件が最後に追加されます。
    /// </remarks>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
    private string BuildOrderBy() => this._orderBys.Count == 0 ? string.Empty : "order by " + string.Join(", ", this._orderBys);

    /// <summary>
    /// 既存のソート条件に追加します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドを降順でソートするための条件を追加します。
    /// 既存のソート条件は保持され、新しい条件が最後に追加されます。
    /// </remarks>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
    private static readonly HashSet<Type> BaseSupportedTypes = [
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal),
        typeof(DateTime), typeof(DateTimeOffset)
    ];

    /// <summary>
    /// 数値型や日付型、文字列、真偽値フィールドでのみ使用可能な型のセットです。
    /// </summary>
    private static readonly HashSet<Type> SupportedTypesForComparison = new HashSet<Type>(BaseSupportedTypes);

    /// <summary>
    /// 数値型や日付型、文字列、真偽値フィールドでのみ使用可能な型のセットです。
    /// </summary>
    private static readonly HashSet<Type> SupportedTypesForEquality = new HashSet<Type>(BaseSupportedTypes) {
        typeof(string),
        typeof(bool)
    };

    /// <summary>
    /// 型がサポートされているかを確認します。
    /// </summary>
    /// <remarks>   
    /// このメソッドは、指定された型がサポートされているかを確認します。
    /// Kintone のクエリでは、数値型や日付型、文字列、真偽値フィールドでのみ使用可能です。
    /// </remarks>
    /// <param name="type">確認する型</param>
    /// <param name="allowStringAndBool">文字列と真偽値を許可するかどうか</param>
    /// <exception cref="NotSupportedException">サポートされていない型の場合にスローされます。</exception>
    /// <exception cref="ArgumentNullException">type が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">allowStringAndBool が true の場合、型がサポートされていない場合にスローされます。</exception>
    private static void ValidateSupportedType(Type type, bool allowStringAndBool = false) {
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

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <param name="field">フィールド名</param>
    /// <param name="from">範囲の開始値</param>
    /// <param name="to">範囲の終了値</param>
    /// <param name="inclusiveLower">開始値を範囲に含む場合は true（&gt;= / &gt;）</param>
    /// <param name="inclusiveUpper">終了値を範囲に含む場合は true（&lt;= / &lt;）</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールド名が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールド名が空文字列の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">from と to の型が一致しない場合にスローされます。</exception>
    /// <exception cref="ArgumentException">from が to より大きい場合にスローされます。</exception>
    /// <exception cref="NotSupportedException">サポートされていない型の場合にスローされます。</exception>
    private KintoneQuery<T> AddBetweenCondition(string field, object from, object to, bool inclusiveLower, bool inclusiveUpper) {
        if (string.IsNullOrWhiteSpace(field)) {
            throw new ArgumentException("Field name must be specified.", nameof(field));
        }

        if (from == null || to == null) {
            throw new ArgumentNullException("from/to cannot be null");
        }

        var valueType = from.GetType();
        if (valueType != to.GetType()) {
            throw new ArgumentException($"from（{valueType.Name}）と to（{to.GetType().Name}）の型は一致している必要があります。");
        }

        // 型のサポート確認
        ValidateSupportedType(valueType, allowStringAndBool: false);

        // 値の比較
        if (Comparer<object>.Default.Compare(from, to) > 0) {
            throw new ArgumentException("from must be less than or equal to to");
        }

        // 演算子選択
        var lowerOp = inclusiveLower ? ">=" : ">";
        var upperOp = inclusiveUpper ? "<=" : "<";

        // フォーマットして条件追加
        var fromStr = FormatValue(from);
        var toStr = FormatValue(to);

        this._conditions.Add($"{field} {lowerOp} {fromStr} and {field} {upperOp} {toStr}");
        return this;
    }

    /// <summary>
    /// フィールド名を取得します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドセレクターからフィールド名を取得します。
    /// </remarks>
    /// <param name="value">フィールドセレクター</param>
    /// <returns>フィールド名</returns>
    /// <exception cref="ArgumentNullException">value が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">value が有効なフィールドを指定していない場合にスローされます。</exception>
    private static string FormatValue(object? value) {
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

    /// <summary>
    /// フィールドの値が指定された値のリストに含まれないかを確認します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドの値が、与えらた値のリストに含まれないかを確認します。
    /// Kintone のクエリでは、フィールドの値が null の場合は、"is null" を使用して確認します。
    /// また、値のリストが空の場合は、常に false を返します。
    /// </remarks>
    /// <typeparam name="TValue">値の型</typeparam>
    /// <param name="fieldSelector">フィールドを指定する式</param>
    /// <param name="values">値のリスト</param>
    /// <param name="negate">否定演算子を使用するかどうか</param>
    /// <returns>KintoneQuery インスタンス</returns>
    /// <exception cref="ArgumentNullException">フィールドセレクターまたは値のリストが null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">フィールドセレクターが有効なフィールドを指定していない場合にスローされます。</exception>
    private KintoneQuery<T> AddInCondition<TValue>(Expression<Func<T, TValue>> fieldSelector, IEnumerable<TValue> values, bool negate) {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ArgumentNullException.ThrowIfNull(values);

        var valueList = values.ToList();
        if (valueList.Count == 0) {
            throw new ArgumentException("値のリストが空です。", nameof(values));
        }

        var type = typeof(TValue);
        ValidateSupportedType(type, allowStringAndBool: true);  // Equal系と同じチェック

        if (fieldSelector.Body is not MemberExpression memberExpr) {
            throw new NotSupportedException("フィールドセレクタは MemberExpression である必要があります");
        }

        var fieldName = memberExpr.Member.Name;

        var formattedValues = valueList.Select(v => FormatValue(v));
        var joinedValues = string.Join(", ", formattedValues);
        var operatorStr = negate ? "not in" : "in";

        this._conditions.Add($"{fieldName} {operatorStr} ({joinedValues})");
        return this;
    }

    /// <summary>
    /// フィールド名を取得します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたフィールドセレクターからフィールド名を取得します。
    /// </remarks>
    /// <param name="keySelector">フィールドセレクター</param>
    /// <returns>フィールド名</returns>
    /// <exception cref="ArgumentNullException">keySelector が null の場合にスローされます。</exception>
    /// <exception cref="ArgumentException">keySelector が有効なフィールドを指定していない場合にスローされます。</exception>
    /// <exception cref="NotSupportedException">フィールドセレクターが MemberExpression でない場合にスローされます。</exception>
    private static string GetFieldName<TValue>(Expression<Func<T, TValue>> keySelector) {
        ArgumentNullException.ThrowIfNull(keySelector);

        MemberExpression? memberExpr = keySelector.Body switch {
            MemberExpression m => m,
            UnaryExpression u when u.NodeType == ExpressionType.Convert => u.Operand as MemberExpression,
            _ => null
        } ?? throw new ArgumentException("無効なフィールド指定です。", nameof(keySelector));
        return memberExpr.Member.Name;
    }
    #endregion
}
