using System.Text.RegularExpressions;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// Python 用名前変換（安全・一貫性・壊れない）
/// </summary>
public class PythonNameConverter : INameConverter {
    // 日本語 → 意味ベース変換（任意）
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
        { "明細", "meisai" },
    };

    // システムフィールド（FieldCode ベース）
    private static readonly HashSet<string> SystemFieldCodes = [
        "CreatedTime", "UpdatedTime", "Creator", "Modifier",
        "Status", "Category", "Assignee"
    ];

    public string ToClassName(string label, string code) {
        var baseName = SelectBaseName(label, code);
        if (Dictionary.TryGetValue(baseName, out var mapped)) {
            return mapped.ToPascalCase();
        }

        // romanize → sanitize → PascalCase
        var roman = baseName.ToRoman();
        var safe = Sanitize(roman);

        if (string.IsNullOrWhiteSpace(safe)) {
            safe = $"App{code}"; // fallback
        }

        return safe.ToPascalCase();
    }

    public string ToPropertyName(string label, string code) {
        var baseName = SelectBaseName(label, code);

        if (string.IsNullOrWhiteSpace(baseName)) {
            return "INVALID_FIELD_NAME";
        }

        // システムフィールドは固定名
        if (SystemFieldCodes.Contains(code)) {
            return code.ToSnakeCase();
        }

        if (code.IsAscii()) {
            return baseName.ToSnakeCase();
        }

        if (Dictionary.TryGetValue(baseName, out var mapped)) {
            return mapped.ToSnakeCase();
        }

        // romanize → sanitize → snake_case
        var roman = baseName.ToRoman();
        var safe = Sanitize(roman);

        if (!string.IsNullOrWhiteSpace(safe)) {
            return safe.ToSnakeCase();
        }

        // プロパティ名を特定できない場合はエラーを出すためのinvalid nameを返す
        return "INVALID_FIELD_NAME";
    }

    /// <summary>
    /// FieldCode を優先し、Label は fallback とする
    /// </summary>
    private static string SelectBaseName(string label, string code) => !string.IsNullOrWhiteSpace(code) ? code : label;

    private static string Sanitize(string text) {
        var sanitized = Regex.Replace(text, @"[^A-Za-z0-9_]", "_");
        sanitized = Regex.Replace(sanitized, "_+", "_");
        return sanitized.Trim('_');
    }
}
