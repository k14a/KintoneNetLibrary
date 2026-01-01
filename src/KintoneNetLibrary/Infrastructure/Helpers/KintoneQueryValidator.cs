using System;
using System.Reflection;
using System.Text.RegularExpressions;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

// コメントは日本語で記述
/// <summary>
/// Kintoneのクエリ文の妥当性を検証するためのヘルパークラス
/// </summary>
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
    /// <summary>
    /// Kintoneのクエリ文に含まれるフィールドコードが、指定されたモデルTに定義されているか検証します。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="throwOnError"></param>
    /// <param name="onWarn"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void ValidateFieldCodes<T>(string query, bool throwOnError = true, Action<string> onWarn = null) {
        if (string.IsNullOrWhiteSpace(query)) {
            return;
        }

        // クエリ中の文字列リテラルを削除（例: "abc" → 空文字に）
        var queryWithoutLiterals = Regex.Replace(query, "\"[^\"]*\"", "");

        // 抽出された単語の候補（重複除去、キーワード除外）
        var candidates = Regex.Matches(queryWithoutLiterals, @"\b\w+\b")
            .Select(m => m.Value)
            .Distinct()
            .Where(w => !IsKintoneQueryKeyword(w))
            .Where(w => !Regex.IsMatch(w, @"^\d+$"))
            .ToList();

        // モデルに定義されているフィールドコード一覧
        var fieldCodes = typeof(T)
            .GetProperties()
            .Select(p => p.GetCustomAttribute<KintoneItemAttribute>())
            .Where(attr => attr != null)
            .Select(attr => attr.FieldCode)
            .ToHashSet();

        // 存在しないフィールドコードを抽出
        var notFound = candidates
            .Where(candidate => !fieldCodes.Contains(candidate))
            .ToList();

        if (notFound.Count != 0) {
            var message = $"[KintoneQueryValidator] クエリ内にKintoneモデルのFieldCodeに存在しないフィールドが含まれています: {string.Join(", ", notFound)}";

            if (throwOnError) {
                throw new InvalidOperationException(message);
            }

            onWarn?.Invoke(message);
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9]+$")]
    private static partial Regex AlphaNumericRegex();
    private static readonly HashSet<string> KintoneKeywords = new(StringComparer.OrdinalIgnoreCase) {
        "and", "or", "not", "in", "like", "contains", "is", "null", "true", "false", "limit", "offset", "order", "by", "asc", "desc"
    };

    /// <summary>
    /// 指定された単語がKintoneのクエリキーワードであるかを判定します。
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    private static bool IsKintoneQueryKeyword(string word) => KintoneKeywords.Contains(word);

    /// <summary>
    /// 指定された単語がリテラル値（数値リテラルまたはクォート済み文字列）であるかを判定します。
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    private static bool IsLiteralValue(string word) {
        // 数字リテラル・クォート済み文字列はここでは単純に除外（実装は要改善可能）
        return int.TryParse(word, out _) || double.TryParse(word, out _) || word.StartsWith("\"") || word.EndsWith("\"");
    }

}
