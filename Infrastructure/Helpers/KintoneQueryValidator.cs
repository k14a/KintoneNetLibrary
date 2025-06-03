using System;
using System.Text.RegularExpressions;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public static partial class KintoneQueryValidator {
    /// <summary>
    /// Kintoneのクエリ文に含まれる like 句が英数字リテラルのみで構成されている場合、警告ログを出力します。
    /// </summary>
    /// <param name="query">Kintoneの検索クエリ</param>
    /// <param name="onWarn">警告を出力するデリゲート（例: msg => logger?.LogWarning(msg)）</param>
    public static void ValidateLikeClause(string? query, Action<string>? onWarn) {
        if (string.IsNullOrWhiteSpace(query) || onWarn == null) {
            return;
        }

        // 正規表現で like "文字列" を抽出（大文字・小文字を無視）
        var regex = new Regex(@"like\s+""([^""]+)""", RegexOptions.IgnoreCase);
        var matches = regex.Matches(query);

        foreach (Match match in matches) {
            if (match.Groups.Count < 2) {
                continue;
            }

            var value = match.Groups[1].Value;

            // 英数字のみで構成されているか（日本語や記号が含まれていないか）
            if (AlphaNumericRegex().IsMatch(value)) {
                onWarn($"[KintoneQueryValidator] クエリ内の like \"{value}\" は英数字のみのため、Kintoneでは部分一致として動作しません（完全一致になります）。");
            }
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9]+$")]
    private static partial Regex AlphaNumericRegex();
}
