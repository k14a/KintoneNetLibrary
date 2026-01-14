using System.Text;
using System.Text.RegularExpressions;
using KintoneNetLibrary.CodeGen.Application.Interfaces;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// 名前変換
/// </summary>
public class CSharpNameConverter : INameConverter {
    private static readonly Dictionary<string, string> Dictionary = new() {
        { "顧客", "Customer" },
        { "担当者", "Assignee" },
        { "日付", "Date" },
        { "日時", "DateTime" },
        { "時間", "Time" },
        { "金額", "Amount" },
        { "数量", "Quantity" },
        { "件名", "Subject" },
        { "備考", "Note" },
        { "名", "Name" },
        // 必要に応じて追加
    };
    private static readonly Dictionary<string, (string PropertyName, string CsType)> SystemFields = new() {
        ["作成者"] = ("Creator", "KintoneUser"),
        ["更新者"] = ("Modifier", "KintoneUser"),
        ["作成日時"] = ("CreatedTime", "DateTime"),
        ["更新日時"] = ("UpdatedTime", "DateTime"),
        ["ステータス"] = ("Status", "string"),
        ["カテゴリー"] = ("Category", "string"),
        ["作業者"] = ("Assignee", "KintoneUser"),
    };


    /// <summary>
    /// クラス名に変換する
    /// </summary>
    /// <param name="label"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public string ToClassName(string label, string code) => MakeSafeIdentifier(this.Convert(label, code));

    /// <summary>
    /// プロパティ名に変換する
    /// </summary>
    /// <param name="label"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public string ToPropertyName(string label, string code) => MakeSafeIdentifier(this.Convert(label, code));

    /// <summary>
    /// 変換ロジック本体
    /// </summary>
    /// <param name="label"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    private string Convert(string label, string code) {
        var baseName = string.IsNullOrWhiteSpace(code) ? label : code;
        if (SystemFields.TryGetValue(baseName, out var systemField)) { return systemField.PropertyName; }

        // 0. フィールドコードを安全化 → PascalCase（最優先）
        var safeCode = SanitizeFieldCode(baseName);
        var codeName = ToPascalCase(safeCode);

        // 1. ラベルが空 → code fallback
        // if (string.IsNullOrWhiteSpace(label)) { return codeName; }

        // 2. ラベルが ASCII → そのまま PascalCase
        if (IsAscii(codeName)) { return ToPascalCase(codeName); }


        // 3. 日本語辞書で完全一致 → 英語化
        if (Dictionary.TryGetValue(codeName, out var mapped)) { return mapped; }

        // 4. ローマ字変換（簡易）
        var roman = ToRoman(codeName);
        if (!string.IsNullOrWhiteSpace(roman)) { return ToPascalCase(roman); }

        // 5. 最後の fallback → code
        return codeName;
    }

    /// <summary>
    /// フィールドコードを安全化する
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    private static string SanitizeFieldCode(string code) {
        if (string.IsNullOrWhiteSpace(code)) { return "_"; }

        // C# の識別子に使えない文字を "_" に置換
        var sanitized = Regex.Replace(code, @"[^A-Za-z0-9_]", "_");

        // 連続する "_" を1つにまとめる
        sanitized = Regex.Replace(sanitized, "_+", "_");

        // 先頭と末尾の "_" を除去
        sanitized = sanitized.Trim('_');

        return sanitized;
    }

    /// <summary>
    /// ASCII 文字列かどうか
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private static bool IsAscii(string s) => s.All(c => c <= 127);

    /// <summary>
    /// PascalCase に変換する
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string ToPascalCase(string text) {
        var parts = Regex.Split(text, @"[^A-Za-z0-9]+")
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Select(x => char.ToUpperInvariant(x[0]) + x[1..]);

        return string.Concat(parts);
    }

    /// <summary>
    /// 安全な識別子に変換する
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string MakeSafeIdentifier(string name) {
        if (string.IsNullOrWhiteSpace(name)) { return "_"; }

        // 先頭が数字なら "_" を付ける
        if (char.IsDigit(name[0])) { name = "_" + name; }

        // C# キーワード回避
        if (IsCSharpKeyword(name.ToLowerInvariant())) { name = "@" + name; }

        return name;
    }

    /// <summary>
    /// C# キーワードかどうか
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static bool IsCSharpKeyword(string name) {
        return new[] {
            "class",
            "namespace",
            "public",
            "private",
            "protected",
            "internal",
            "string",
            "int",
            "decimal",
            "var"
        }.Contains(name);
    }

    /// <summary>
    /// 簡易ローマ字変換
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string ToRoman(string text) {
        if (string.IsNullOrEmpty(text)) { return string.Empty; }

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
