using System.Text.RegularExpressions;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.Extensions;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// Python 用名前変換（安全・一貫性・壊れない）
/// </summary>
public class PythonNameConverter : INameConverter, INameTableApplicable {
    private NameTable? _nameTable;

    /// <summary>
    /// 日本語フィールド名を意味ベースで英語に変換するための辞書
    /// </summary>
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

    /// <summary>
    /// Kintone のシステムフィールドコードのセット。これらは固定のプロパティ名にマッピングされるべきで、変換テーブルや一般的なルールの対象外とするために定義しています。
    /// </summary>
    private static readonly HashSet<string> SystemFieldCodes = [
        "CreatedTime", "UpdatedTime", "Creator", "Modifier",
        "Status", "Category", "Assignee"
    ];

    /// <summary>
    /// 変換テーブルを適用します。
    /// これにより、変換テーブルに基づいた名前変換が優先されるようになります。
    /// 例えば、フィールドコード "customer" に対して、変換テーブルで "CustomerName" とマッピングされていれば、
    /// ToPropertyName("customer", "顧客") は "CustomerName" を返すようになります。
    /// 変換テーブルにないフィールドコードは、従来のロジックで変換されます。
    /// </summary>
    /// <param name="table"></param>
    public void LoadNameTable(NameTable table) {
        this._nameTable = table;
    }

    /// <summary>
    /// クラス名に変換する
    /// </summary>
    /// <param name="label">ラベル</param>
    /// <param name="code">コード</param>
    /// <returns>変換後のクラス名</returns>
    public string ToClassName(string label, string code, bool tableTemplate = false) {
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

    /// <summary>
    /// プロパティ名に変換する
    /// </summary>
    /// <param name="label">ラベル</param>
    /// <param name="code">コード</param>
    /// <returns>変換後のプロパティ名</returns>
    public string ToPropertyName(string label, string code, bool tableTemplate = false) {
        if (this._nameTable != null) {
            var mapping = this._nameTable.TryGet(code);
            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.Property)) {
                return mapping.Property;
            }
        }

        var baseName = SelectBaseName(label, code);
        if (string.IsNullOrWhiteSpace(baseName)) { return "INVALID_FIELD_NAME"; }

        if (baseName.IsAscii()) { return baseName.ToSnakeCase(); }

        if (tableTemplate) { return string.Empty; }

        // システムフィールドは固定名
        if (SystemFieldCodes.Contains(code)) { return code.ToSnakeCase(); }

        if (code.IsAscii()) { return baseName.ToSnakeCase(); }

        if (Dictionary.TryGetValue(baseName, out var mapped)) { return mapped.ToSnakeCase(); }

        // romanize → sanitize → snake_case
        var roman = baseName.ToRoman();
        var safe = Sanitize(roman);

        if (!string.IsNullOrWhiteSpace(safe)) { return safe.ToSnakeCase(); }

        // プロパティ名を特定できない場合はエラーを出すためのinvalid nameを返す
        return tableTemplate ? string.Empty : "INVALID_FIELD_NAME";
    }

    /// <summary>
    /// ベース名を選択する（code があれば code、なければ label）
    /// </summary>
    /// <param name="label">ラベル</param>
    /// <param name="code">コード</param>
    /// <returns>ベース名</returns>
    private static string SelectBaseName(string label, string code) => !string.IsNullOrWhiteSpace(code) ? code : label;

    /// <summary>
    /// 文字列を安全な識別子に変換する
    /// </summary>
    /// <param name="text">変換対象の文字列</param>
    /// <returns>安全な識別子</returns>
    private static string Sanitize(string text) {
        var sanitized = Regex.Replace(text, @"[^A-Za-z0-9_]", "_");
        sanitized = Regex.Replace(sanitized, "_+", "_");
        return sanitized.Trim('_');
    }
}
