using System.Text;
using System.Text.RegularExpressions;
using KintoneNetLibrary.CodeGen.Application.Interfaces;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

public class NameConverter : INameConverter {
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

    public string ToClassName(string label, string code) {
        var name = this.Convert(label, code);
        return MakeSafeIdentifier(name);
    }

    public string ToPropertyName(string label, string code) {
        var name = this.Convert(label, code);
        return MakeSafeIdentifier(name);
    }

    private string Convert(string label, string code) {
        if(string.IsNullOrWhiteSpace(label)) {
            return ToPascalCase(code);
        }

        // 1. Label を辞書で英語化
        foreach (var kv in Dictionary) {
            if (label.Contains(kv.Key)) {
                // return kv.Value;
                label = label.Replace(kv.Key, kv.Value);
            }
        }

        // 2. Label が英語ならそのまま
        if (IsAscii(label)) { return ToPascalCase(label); }

        // 3. ローマ字変換（簡易）
        var roman = ToRoman(label);
        if (!string.IsNullOrWhiteSpace(roman)) {
            return ToPascalCase(roman);
        }

        // 4. 最後の fallback → code を PascalCase
        return ToPascalCase(code);
    }

    private static bool IsAscii(string s) => s.All(c => c <= 127);

    private static string ToPascalCase(string text) {
        var parts = Regex.Split(text, @"[^A-Za-z0-9]+")
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1));

        return string.Concat(parts);
    }

    private static string MakeSafeIdentifier(string name) {
        if (string.IsNullOrWhiteSpace(name)) { return "_"; }

        // 先頭が数字なら _ を付ける
        if (char.IsDigit(name[0])) { name = "_" + name; }

        // C# キーワード回避
        if (IsCSharpKeyword(name.ToLowerInvariant())) { name = "@" + name; }

        return name;
    }

    private static bool IsCSharpKeyword(string name) {
        return new[] {
            "class", "namespace", "public", "private", "protected",
            "internal", "string", "int", "decimal", "var"
        }.Contains(name);
    }

    private static string ToRoman(string text) {
        // 簡易ローマ字変換（必要なら後で強化）
        var sb = new StringBuilder();
        foreach (var c in text) {
            sb.Append(c switch {
                'あ' => "a", 'い' => "i", 'う' => "u", 'え' => "e", 'お' => "o",
                'か' => "ka", 'き' => "ki", 'く' => "ku", 'け' => "ke", 'こ' => "ko",
                'さ' => "sa", 'し' => "shi", 'す' => "su", 'せ' => "se", 'そ' => "so",
                _ => ""
            });
        }
        return sb.ToString();
    }
}
