using System.Text;
using System.Text.RegularExpressions;
using KintoneNetLibrary.CodeGen.Application.Interfaces;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// Python 用名前変換
/// </summary>
public class PythonNameConverter : INameConverter {
    private static readonly Dictionary<string, string> Dictionary = new() {
        { "顧客", "customer" },
        { "担当者", "assignee" },
        { "日付", "date" },
        { "日時", "datetime" },
        { "時間", "time" },
        { "金額", "amount" },
        { "数量", "quantity" },
        { "件名", "subject" },
        { "備考", "note" },
        { "名", "name" },
    };

    private static readonly Dictionary<string, string> SystemFields = new() {
        ["作成者"] = "creator",
        ["更新者"] = "modifier",
        ["作成日時"] = "created_time",
        ["更新日時"] = "updated_time",
        ["ステータス"] = "status",
        ["カテゴリー"] = "category",
        ["作業者"] = "assignee",
    };

    public string ToClassName(string label, string code) {
        var baseName = this.ConvertInternal(label, code);
        return ToPascalCase(baseName);
    }

    public string ToPropertyName(string label, string code) {
        var baseName = this.ConvertInternal(label, code);
        return ToSnakeCase(baseName);
    }

    private string ConvertInternal(string label, string code) {
        var baseName = string.IsNullOrWhiteSpace(code) ? label : code;

        if (SystemFields.TryGetValue(baseName, out var sys)) { return sys; }

        var safe = Sanitize(baseName);

        if (IsAscii(safe)) { return safe.ToLowerInvariant(); }

        if (Dictionary.TryGetValue(baseName, out var mapped)) { return mapped; }

        var roman = ToRoman(baseName);
        if (!string.IsNullOrWhiteSpace(roman)) { return roman.ToLowerInvariant(); }

        return safe.ToLowerInvariant();
    }

    private static string Sanitize(string text) {
        var sanitized = Regex.Replace(text, @"[^A-Za-z0-9_]", "_");
        sanitized = Regex.Replace(sanitized, "_+", "_");
        return sanitized.Trim('_');
    }

    private static bool IsAscii(string s) => s.All(c => c <= 127);

    private static string ToPascalCase(string text) {
        var parts = Regex.Split(text, @"[^A-Za-z0-9]+")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant());

        return string.Concat(parts);
    }

    private static string ToSnakeCase(string text) {
        var parts = Regex.Split(text, @"[^A-Za-z0-9]+")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToLowerInvariant());

        return string.Join("_", parts);
    }

    private static string ToRoman(string text) {
        // CSharpNameConverter のローマ字変換をそのまま流用
        return CSharpNameConverter.ToRoman(text);
    }
}
