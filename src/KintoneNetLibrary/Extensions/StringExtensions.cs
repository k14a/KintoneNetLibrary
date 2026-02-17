using System.Text;
using System.Text.RegularExpressions;

namespace KintoneNetLibrary.Extensions;

/// <summary>
/// 文字列操作に関する拡張メソッドを提供します。
/// </summary>
public static class StringExtensions {
    /// <summary>
    /// 文字列が ASCII 文字のみで構成されているかどうかを判定します。
    /// </summary>
    /// <param name="s">判定対象の文字列</param>
    /// <returns>ASCII 文字のみで構成されている場合は true、それ以外の場合は false</returns>
    public static bool IsAscii(this string s) => s.All(c => c <= 127);
    /// <summary>
    /// 文字列を snake_case に変換します。
    /// </summary>
    /// <param name="text">変換対象の文字列</param>
    /// <returns>変換後の snake_case 文字列</returns>
    public static string ToSnakeCase(this string text) {
        if (string.IsNullOrWhiteSpace(text)) { return text; }

        // CamelCase / PascalCase → snake_case
        // 例: "AddedTextBox" → "Added_Text_Box"
        var withUnderscore = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1_$2");

        // 英数字以外は区切り扱い
        var parts = Regex.Split(withUnderscore, @"[^A-Za-z0-9]+").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.ToLowerInvariant());

        return string.Join("_", parts);
    }
    /// <summary>
    /// 文字列を PascalCase に変換します。
    /// </summary>
    /// <param name="text">変換対象の文字列</param>
    /// <returns>変換後の PascalCase 文字列</returns>
    public static string ToPascalCase(this string text) {
        var parts = Regex.Split(text, @"[^A-Za-z0-9]+").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => char.ToUpperInvariant(x[0]) + x[1..]);

        return string.Concat(parts);
    }
    /// <summary>
    /// 文字列を camelCase に変換します。
    /// </summary>
    /// <param name="text">変換対象の文字列</param>
    /// <returns>変換後の camelCase 文字列</returns>
    public static string ToCamelCase(this string text) {
        var parts = Regex.Split(text, @"[^A-Za-z0-9]+").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        if (parts.Count == 0) { return string.Empty; }

        var firstPart = parts[0].ToLowerInvariant();
        var remainingParts = parts.Skip(1).Select(x => char.ToUpperInvariant(x[0]) + x[1..]);

        return firstPart + string.Concat(remainingParts);
    }
    /// <summary>
    /// 文字列をローマ字に変換します。
    /// </summary>
    /// <param name="text">変換対象の文字列</param>
    /// <returns>変換後のローマ字文字列</returns>
    public static string ToRoman(this string text) {
        if (string.IsNullOrEmpty(text)) { return string.Empty; }
        if (text.IsAscii()) { return text; }

        var sb = new StringBuilder();

        // 変換マップの定義（2文字の拗音を先に定義する）
        var map = new Dictionary<string, string> {
            // 拗音（2文字）
            {"きゃ", "kya"}, {"きゅ", "kyu"}, {"きょ", "kyo"},
            {"しゃ", "sha"}, {"しゅ", "shu"}, {"しょ", "sho"},
            {"ちゃ", "cha"}, {"ちゅ", "chu"}, {"ちょ", "cho"},
            {"にゃ", "nya"}, {"にゅ", "nyu"}, {"にょ", "nyo"},
            {"ひゃ", "hya"}, {"ひゅ", "hyu"}, {"ひょ", "hyo"},
            {"みゃ", "mya"}, {"みゅ", "myu"}, {"みょ", "myo"},
            {"りゃ", "rya"}, {"りゅ", "ryu"}, {"りょ", "ryo"},
            {"ぎゃ", "gya"}, {"ぎゅ", "gyu"}, {"ぎょ", "gyo"},
            {"じゃ", "ja"},  {"じゅ", "ju"},  {"じょ", "jo"},
            {"びゃ", "bya"}, {"びゅ", "byu"}, {"びょ", "byo"},
            {"ぴゃ", "pya"}, {"ぴゅ", "pyu"}, {"ぴょ", "pyo"},

            // 1文字
            {"あ", "a"},  {"い", "i"},   {"う", "u"},   {"え", "e"},  {"お", "o"},
            {"か", "ka"}, {"き", "ki"},  {"く", "ku"},  {"け", "ke"}, {"こ", "ko"},
            {"さ", "sa"}, {"し", "shi"}, {"す", "su"},  {"せ", "se"}, {"そ", "so"},
            {"た", "ta"}, {"ち", "chi"}, {"つ", "tsu"}, {"て", "te"}, {"と", "to"},
            {"な", "na"}, {"に", "ni"},  {"ぬ", "nu"},  {"ね", "ne"}, {"の", "no"},
            {"は", "ha"}, {"ひ", "hi"},  {"ふ", "fu"},  {"へ", "he"}, {"ほ", "ho"},
            {"ま", "ma"}, {"み", "mi"},  {"む", "mu"},  {"め", "me"}, {"も", "mo"},
            {"や", "ya"}, {"ゆ", "yu"},  {"よ", "yo"},
            {"ら", "ra"}, {"り", "ri"},  {"る", "ru"},  {"れ", "re"}, {"ろ", "ro"},
            {"わ", "wa"}, {"を", "wo"},  {"ん", "n"},

            // 濁音・半濁音
            {"が", "ga"}, {"ぎ", "gi"}, {"ぐ", "gu"}, {"げ", "ge"}, {"ご", "go"},
            {"ざ", "za"}, {"じ", "ji"}, {"ず", "zu"}, {"ぜ", "ze"}, {"ぞ", "zo"},
            {"だ", "da"}, {"ぢ", "ji"}, {"づ", "zu"}, {"で", "de"}, {"ど", "do"},
            {"ば", "ba"}, {"び", "bi"}, {"ぶ", "bu"}, {"べ", "be"}, {"ぼ", "bo"},
            {"ぱ", "pa"}, {"ぴ", "pi"}, {"ぷ", "pu"}, {"ぺ", "pe"}, {"ぽ", "po"},

            // 特殊記号
            {"ー", "-"}, {"っ", ""} // 「っ」は次の文字で判定するためここでは空
        };

        for (int i = 0; i < text.Length; i++) {
            if (i + 1 < text.Length && map.TryGetValue(text.Substring(i, 2), out var doubleChar)) {
                // 1. 2文字の組み合わせ（拗音）をチェック
                sb.Append(doubleChar);
                i++; // 2文字分進める
            } else if (text[i] == 'っ' && i + 1 < text.Length) {
                // 2. 「っ」の処理（次の文字の最初の子音を重ねる）
                // 次の文字を1文字チェックして、そのローマ字の先頭を重ねる
                if (map.TryGetValue(text.Substring(i + 1, 1), out var next)) {
                    sb.Append(next[0]);
                }
            } else if (map.TryGetValue(text[i].ToString(), out var singleChar)) {
                // 3. 通常の1文字チェック
                sb.Append(singleChar);
            }
        }

        return sb.ToString();
    }
}
