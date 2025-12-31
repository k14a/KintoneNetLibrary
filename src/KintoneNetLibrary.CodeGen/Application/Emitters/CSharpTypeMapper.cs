using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

public class CSharpTypeMapper : ITypeMapper {
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
}
  