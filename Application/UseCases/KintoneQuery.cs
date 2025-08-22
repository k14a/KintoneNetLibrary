using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Application.UseCases;

/// <summary>
/// KintoneQuery クラスは、Kintone アプリのレコードを取得するためのクエリを構築します。
/// </summary>
/// <remarks>
/// このクラスは、Kintone アプリのレコードを取得するためのクエリを構築します。
/// クエリの条件、並び順、制限などを設定し、最終的にクエリ文字列を生成します。
/// クエリの構築には、LINQ の式木を使用して型安全にフィールド名を取得します。
/// また、クエリの構築においては、Kintone の制約に従い、特定のキーワード（例: "offset"）を使用しないように注意が必要です。
/// </remarks>
/// <typeparam name="T">KintoneModelBase&lt;T&gt; を継承したモデル型</typeparam>
public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Private values>>
    private readonly List<string> _conditions = [];
    private readonly List<string> _orderBys = [];
    private string? _orderBy;
    private int? _limit;
    private int? _offset;
    #endregion

    #region <<Properties>>
    /// <summary>
    /// クエリの条件を追加します。
    /// </summary>
    /// <remarks>条件は、Kintone のフィールドコードを使用して指定します。</remarks>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
    #endregion

    #region <<Constructor(s)>>
    /// <summary>
    /// KintoneQuery のコンストラクタ
    /// </summary>
    /// <remarks>このコンストラクタは、KintoneQuery クラスのインスタンスを初期化します。</remarks>
    /// <exception cref="ArgumentNullException">引数が null の場合にスローされます。</exception>
    public KintoneQuery() { }
    #endregion

    #region <<Public methods>>
    /// <summary>
    /// クエリの条件を追加します。
    /// </summary>
    /// <remarks>条件は、Kintone のフィールドコードを使用して指定します。</remarks>
    /// <exception cref="ArgumentNullException">条件が null の場合にスローされます。</exception>
    public string Build() {
        var queryParts = new List<string>();

        // 条件の構築
        if (this._conditions.Count > 0) {
            var conditionPart = string.Join(" and ", this._conditions);

            // "offset" を単語として含むかをチェック
            if (Regex.IsMatch(conditionPart, @"\boffset\b", RegexOptions.IgnoreCase)) {
                throw new KintoneException("'offset' は使用できません。カーソルAPIを利用してください。");
            }

            queryParts.Add(conditionPart);
        }

        // order by句の追加（先頭スペースなし）
        var orderBy = this.BuildOrderBy(); // "order by ..." or ""
        if (!string.IsNullOrEmpty(orderBy)) {
            queryParts.Add(orderBy);
        }

        // limit句の追加
        if (this._limit.HasValue) {
            queryParts.Add($"limit {this._limit.Value}");
        }

        // 各句をスペースで連結
        return string.Join(" ", queryParts);
    }

    /// <summary>
    /// クエリの条件を追加します。
    /// </summary>
    public override string ToString() => this.Build();
    #endregion
}
