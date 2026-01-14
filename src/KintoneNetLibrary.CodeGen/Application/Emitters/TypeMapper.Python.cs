using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// Python 型マッパー
/// </summary>
public class PythonTypeMapper : ITypeMapper {
    public string Map(KintoneFieldMetadata field) {
        return field.Type switch {
            // 文字列系
            KintoneFieldType.SingleLineText => "str",
            KintoneFieldType.MultiLineText => "str",
            KintoneFieldType.RichText => "str",

            // 数値
            KintoneFieldType.Number => "str",

            // 日付・日時 → KintoneDateTime
            KintoneFieldType.Date => "str",
            KintoneFieldType.DateTime => "str",
            KintoneFieldType.Time => "str",

            // 選択肢系
            KintoneFieldType.CheckBox => "list[str]",
            KintoneFieldType.MultiSelect => "list[str]",
            KintoneFieldType.RadioButton => "str",
            KintoneFieldType.DropDown => "str",

            // ファイル
            KintoneFieldType.File => "list[str]",

            // ユーザー選択
            KintoneFieldType.UserSelect => "list[dict]",
            KintoneFieldType.GroupSelect => "list[dict]",
            KintoneFieldType.OrganizationSelect => "list[dict]",

            // リンク系
            KintoneFieldType.LinkUrl => "str",
            KintoneFieldType.LinkTelephone => "str",
            KintoneFieldType.LinkEmail => "str",

            // サブテーブル → list[クラス名]
            KintoneFieldType.SubTable => $"list[{field.Code}]",

            // fallback
            _ => "str"
        };
    }

    /// <summary>
    /// Kintone フィールドスキーマを Python 型にマップする
    /// </summary>
    /// <param name="field"></param>
    /// <param name="useLibrary"></param>
    /// <param name="subTableClassName"></param>
    /// <returns></returns>
    public string MapType(KintoneFieldSchema field, bool useLibrary, string subTableClassName = "") {
        return field.FieldType switch {
            // 文字列系
            KintoneFieldType.SingleLineText => "str",
            KintoneFieldType.MultiLineText => "str",
            KintoneFieldType.RichText => "str",

            // 数値
            KintoneFieldType.Number => "str",

            // 日付・日時 → KintoneDateTime
            KintoneFieldType.Date => "str",
            KintoneFieldType.DateTime => "str",
            KintoneFieldType.Time => "str",

            // 選択肢系
            KintoneFieldType.CheckBox => "list[str]",
            KintoneFieldType.MultiSelect => "list[str]",
            KintoneFieldType.RadioButton => "str",
            KintoneFieldType.DropDown => "str",

            // ファイル
            KintoneFieldType.File => "list[str]",

            // ユーザー選択
            KintoneFieldType.UserSelect => "list[dict]",
            KintoneFieldType.GroupSelect => "list[dict]",
            KintoneFieldType.OrganizationSelect => "list[dict]",

            // リンク系
            KintoneFieldType.LinkUrl => "str",
            KintoneFieldType.LinkTelephone => "str",
            KintoneFieldType.LinkEmail => "str",

            // サブテーブル → list[クラス名]
            KintoneFieldType.SubTable => $"list[{field.FieldCode}]",

            // fallback
            _ => "str"
        };
    }
}