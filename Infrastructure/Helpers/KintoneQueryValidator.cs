using System;
using System.Reflection;
using System.Text.RegularExpressions;
using KintoneNetLibrary.Domain.Entities;

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
    /// <summary>
    /// 指定モデルの KintoneItemAttribute.FieldCode に基づき、クエリ内に存在しないフィールドコードが使われていないか検証します。
    /// </summary>
    /// <typeparam name="T">KintoneModelBaseを継承したモデル型</typeparam>
    /// <param name="query">検証対象のクエリ</param>
    /// <param name="throwOnError">trueの場合、無効フィールドがあれば例外をスロー。falseならログ警告のみ。</param>
    /// <param name="onWarn">警告ログ出力用デリゲート。nullなら出力しない。</param>
    // public static void ValidateFieldCodes<T>(string? query, bool throwOnError = true, Action<string>? onWarn = null) where T : KintoneModelBase {
    //     if (string.IsNullOrWhiteSpace(query)) {
    //         return;
    //     }

    //     // モデルの FieldCode を取得
    //     var validFieldCodes = typeof(T).GetProperties()
    //         .Select(p => p.GetCustomAttribute<KintoneItemAttribute>())
    //         .Where(attr => attr != null)
    //         .Select(attr => attr!.FieldCode)
    //         .Where(code => !string.IsNullOrWhiteSpace(code))
    //         .Select(code => code!.Trim())
    //         .ToHashSet(StringComparer.OrdinalIgnoreCase);

    //     if (validFieldCodes.Count == 0) {
    //         // FieldCode設定が無いモデルは検証不可なのでスキップ
    //         return;
    //     }

    //     // クエリからフィールド名を抽出（ざっくり正規表現: 文字列リテラルや比較演算子周辺で分割など、要調整）
    //     // ここでは単純に単語を抽出し、数字リテラルやキーワードを除外する例
    //     var candidates = Regex.Matches(query, @"\b\w+\b")
    //         .Select(m => m.Value)
    //         .Distinct()
    //         .Where(w => !IsKintoneQueryKeyword(w))
    //         .Where(w => !IsLiteralValue(w))
    //         .ToList();

    //     // クエリ中に存在しているが、FieldCodeに無いものを抽出
    //     var invalidFields = candidates.Where(f => !validFieldCodes.Contains(f)).ToList();

    //     if (invalidFields.Any()) {
    //         var message = $"[KintoneQueryValidator] クエリ内にKintoneモデルのFieldCodeに存在しないフィールドが含まれています: {string.Join(", ", invalidFields)}";

    //         if (throwOnError) {
    //             throw new InvalidOperationException(message);
    //         } else {
    //             onWarn?.Invoke(message);
    //         }
    //     }
    // }
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

        if (notFound.Any()) {
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

    private static bool IsKintoneQueryKeyword(string word) => KintoneKeywords.Contains(word);

    private static bool IsLiteralValue(string word) {
        // 数字リテラル・クォート済み文字列はここでは単純に除外（実装は要改善可能）
        return int.TryParse(word, out _) || double.TryParse(word, out _) || word.StartsWith("\"") || word.EndsWith("\"");
    }

}
