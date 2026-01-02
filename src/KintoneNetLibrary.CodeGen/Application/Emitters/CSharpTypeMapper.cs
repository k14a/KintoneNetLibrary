using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// C# 型マッパー
/// </summary>
public class CSharpTypeMapper : ITypeMapper {
    /// <summary>
    /// Kintone フィールドを C# 型にマップする
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public string Map(KintoneFieldMetadata field) {
        return field.Type switch {
            // 文字列系
            KintoneFieldType.SingleLineText => "string",
            KintoneFieldType.MultiLineText => "string",
            KintoneFieldType.RichText => "string",

            // 数値
            KintoneFieldType.Number => "decimal?",

            // 日付・日時 → KintoneDateTime
            KintoneFieldType.Date => "KintoneDateTime",
            KintoneFieldType.DateTime => "KintoneDateTime",

            // 時刻 → KintoneTimeOnly
            KintoneFieldType.Time => "KintoneTimeOnly",

            // 選択肢系
            KintoneFieldType.CheckBox => "List<string>",
            KintoneFieldType.RadioButton => "string",
            KintoneFieldType.DropDown => "string",
            KintoneFieldType.MultiSelect => "List<string>",

            // ファイル
            KintoneFieldType.File => "List<KintoneFile>",

            // ユーザー選択
            KintoneFieldType.UserSelect => "List<KintoneUser>",
            KintoneFieldType.GroupSelect => "List<KintoneGroup>",
            KintoneFieldType.OrganizationSelect => "List<KintoneOrganization>",

            // サブテーブル → List<クラス名>
            KintoneFieldType.SubTable => $"List<{field.Code}>",

            // fallback
            _ => "string"
        };
    }

    public string MapType(KintoneFieldSchema field, bool useLibrary, string subtableClassName = "") {
        if (field.FieldType == KintoneFieldType.SubTable) {
            return $"List<{subtableClassName}>";
        }

        return useLibrary ? this.MapLibrary(field) : this.MapPure(field);
    }
    private string MapLibrary(KintoneFieldSchema field) {
        return field.FieldType switch {
            KintoneFieldType.SingleLineText => "string",
            KintoneFieldType.MultiLineText => "string",
            KintoneFieldType.RichText => "string",

            KintoneFieldType.Number => field.DecimalPlaces > 0 ? "decimal?" : "int?",

            KintoneFieldType.Calc => "string",

            KintoneFieldType.Date => "KintoneDateTime",
            KintoneFieldType.DateTime => "KintoneDateTime",
            KintoneFieldType.Time => "KintoneTimeOnly",
            KintoneFieldType.CheckBox => "List<string>",
            KintoneFieldType.MultiSelect => "List<string>",
            KintoneFieldType.RadioButton => "string",
            KintoneFieldType.DropDown => "string",

            KintoneFieldType.UserSelect => "List<KintoneUser>",
            KintoneFieldType.OrganizationSelect => "List<string>",
            KintoneFieldType.GroupSelect => "List<string>",

            KintoneFieldType.File => "List<KintoneFile>",

            KintoneFieldType.LinkUrl => "string",
            KintoneFieldType.LinkTelephone => "string",
            KintoneFieldType.LinkEmail => "string",

            _ => "string",
        };
    }
    private string MapPure(KintoneFieldSchema field) {
        return field.FieldType switch {
            KintoneFieldType.SingleLineText => "string",
            KintoneFieldType.MultiLineText => "string",
            KintoneFieldType.RichText => "string",

            KintoneFieldType.Number => field.DecimalPlaces > 0 ? "decimal?" : "int?",

            KintoneFieldType.Calc => "string",

            KintoneFieldType.Date => "DateOnly?",
            KintoneFieldType.DateTime => "DateTime?",
            KintoneFieldType.Time => "TimeOnly?",

            KintoneFieldType.CheckBox => "List<string>",
            KintoneFieldType.MultiSelect => "List<string>",
            KintoneFieldType.RadioButton => "string",
            KintoneFieldType.DropDown => "string",

            KintoneFieldType.UserSelect => "List<UserInfo>",
            KintoneFieldType.OrganizationSelect => "List<GroupInfo>",
            KintoneFieldType.GroupSelect => "List<OrganizationInfo>",

            KintoneFieldType.File => "List<string>",

            KintoneFieldType.LinkUrl => "string",
            KintoneFieldType.LinkTelephone => "string",
            KintoneFieldType.LinkEmail => "string",

            _ => "string"
        };
    }
}
  