using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// Python 型マッパー（安全・一貫性・壊れない）
/// </summary>
public class PythonTypeMapper : ITypeMapper {
    public string Map(KintoneFieldMetadata field) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Kintone フィールドスキーマを Python 型にマップする
    /// </summary>
    public string MapType(KintoneFieldSchema field, bool useTypeHint, string subTableClassName = "") {
        // 型ヒントを使わない場合は全部 str にする
        if (!useTypeHint) { return "str"; }

        return field.FieldType switch {
            // 文字列系
            KintoneFieldType.SingleLineText => "str",
            KintoneFieldType.MultiLineText => "str",
            KintoneFieldType.RichText => "str",

            // 数値
            KintoneFieldType.Number => "int | None",

            // 日付・日時
            KintoneFieldType.Date => "datetime.date | None",
            KintoneFieldType.Time => "datetime.time | None",
            KintoneFieldType.DateTime => "datetime.datetime | None",

            // 選択肢系
            KintoneFieldType.CheckBox => "list[str]",
            KintoneFieldType.MultiSelect => "list[str]",
            KintoneFieldType.RadioButton => "str",
            KintoneFieldType.DropDown => "str",

            // ファイル
            KintoneFieldType.File => "list[File]",

            // ユーザー選択
            KintoneFieldType.UserSelect => "list[User]",
            KintoneFieldType.GroupSelect => "list[Group]",
            KintoneFieldType.OrganizationSelect => "list[Organization]",

            // リンク系
            KintoneFieldType.LinkUrl => "str",
            KintoneFieldType.LinkTelephone => "str",
            KintoneFieldType.LinkEmail => "str",

            // サブテーブル
            KintoneFieldType.SubTable =>
                string.IsNullOrWhiteSpace(subTableClassName)
                    ? "list[dict]" // fallback
                    : $"list[{subTableClassName}]",

            // fallback
            _ => "str"
        };
    }
}
