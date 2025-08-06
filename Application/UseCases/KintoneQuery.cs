using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Application.UseCases;

public partial class KintoneQuery<T> where T : KintoneModelBase<T>, new() {
    #region <<Private values>>
    private readonly List<string> _conditions = new List<string>();
    private readonly List<string> _orderBys = new();
    private string? _orderBy;
    private int? _limit;
    private int? _offset;
    #endregion

    #region <<Properties>>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
    #endregion

    #region <<Constructor(s)>>
    public KintoneQuery() { }
    #endregion

    #region <<Public methods>>
    public string Build() {
        var queryParts = new List<string>();

        // 条件の構築
        if (_conditions.Count > 0) {
            var conditionPart = string.Join(" and ", _conditions);

            // "offset" を単語として含むかをチェック
            if (Regex.IsMatch(conditionPart, @"\boffset\b", RegexOptions.IgnoreCase)) {
                throw new KintoneException("'offset' は使用できません。カーソルAPIを利用してください。");
            }

            queryParts.Add(conditionPart);
        }

        // order by句の追加（先頭スペースなし）
        var orderBy = BuildOrderBy(); // "order by ..." or ""
        if (!string.IsNullOrEmpty(orderBy)) {
            queryParts.Add(orderBy);
        }

        // limit句の追加
        if (_limit.HasValue) {
            queryParts.Add($"limit {_limit.Value}");
        }

        // 各句をスペースで連結
        return string.Join(" ", queryParts);
    }
    public override string ToString() => this.Build();
    #endregion

    #region <<Private methods>>
    #endregion
}
