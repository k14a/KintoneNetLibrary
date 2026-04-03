using System.Text.RegularExpressions;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// 名前変換
/// </summary>
public class CSharpNameConverter : INameConverter, INameTableApplicable {
    private NameTable? _nameTable;
    /// <summary>
    /// 日本語フィールド名を意味ベースで英語に変換するための辞書
    /// </summary>
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
    /// <summary>
    /// Kintone のシステムフィールドコードのセット。これらは固定のプロパティ名にマッピングされるべきで、変換テーブルや一般的なルールの対象外とするために定義しています。
    /// </summary>
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
    /// 変換テーブルを適用します。
    /// これにより、変換テーブルに基づいた名前変換が優先されるようになります。
    /// 例えば、フィールドコード "customer" に対して、変換テーブルで "CustomerName" とマッピングされていれば、
    /// ToPropertyName("customer", "顧客") は "CustomerName" を返すようになります。
    /// 変換テーブルにないフィールドコードは、従来のロジックで変換されます。
    /// </summary>
    /// <param name="table">適用する変換テーブル</param>
    public void LoadNameTable(NameTable table) {
        this._nameTable = table;
    }

    /// <summary>
    /// クラス名に変換する
    /// </summary>
    /// <param name="label">ラベル</param>
    /// <param name="code">コード</param>
    /// <param name="tableTemplate">サブテーブルテンプレートかどうか</param>
    /// <returns>変換後のクラス名</returns>
    public string ToClassName(string label, string code, bool tableTemplate = false) {
        var baseName = string.IsNullOrWhiteSpace(code) ? label : code;
        var mapping = this._nameTable?.TryGet(baseName);
        if (!string.IsNullOrWhiteSpace(mapping?.Property)) {
            return mapping.Property;
        }

        var converted = this.Convert(label, code, tableTemplate);
        return MakeSafeIdentifier(converted);
    }

    /// <summary>
    /// プロパティ名に変換する
    /// </summary>
    /// <param name="label">ラベル</param>
    /// <param name="code">コード</param>
    /// <param name="tableTemplate">サブテーブルテンプレートかどうか</param>
    /// <returns>変換後のプロパティ名</returns>
    public string ToPropertyName(string label, string code, bool tableTemplate = false) {
        var mapping = this._nameTable?.TryGet(code);
        if (!string.IsNullOrWhiteSpace(mapping?.Property)) {
            return mapping.Property;
        }

        var converted = this.Convert(label, code, tableTemplate);
        return MakeSafeIdentifier(converted);
    }

    /// <summary>
    /// 変換ロジック本体
    /// </summary>
    /// <param name="label">ラベル</param>
    /// <param name="code">コード</param>
    /// <returns>変換後の名前</returns>
    private string Convert(string label, string code, bool tableTemplate = false) {
        var baseName = string.IsNullOrWhiteSpace(code) ? label : code;
        if (SystemFields.TryGetValue(baseName, out var systemField)) { return systemField.PropertyName; }
        if (baseName.IsAscii()) { return baseName.ToPascalCase(); }
        if (tableTemplate) { return string.Empty; }

        var safeCode = SanitizeFieldCode(baseName);
        var codeName = safeCode.ToPascalCase();

        if (Dictionary.TryGetValue(codeName, out var mapped)) { return mapped; }

        var roman = codeName.ToRoman();
        if (!string.IsNullOrWhiteSpace(roman)) { return roman.ToPascalCase(); }

        return codeName;
    }

    /// <summary>
    /// フィールドコードを安全化する
    /// </summary>
    /// <param name="code">フィールドコード</param>
    /// <returns>安全化されたフィールドコード</returns>
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
    /// 安全な識別子に変換する
    /// </summary>
    /// <param name="name">識別子名</param>
    /// <returns>安全な識別子</returns>
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
    /// <param name="name">識別子名</param>
    /// <returns>キーワードの場合は true、それ以外は false</returns>
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

}
