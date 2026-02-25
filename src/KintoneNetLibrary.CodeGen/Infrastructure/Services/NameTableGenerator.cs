using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

/// <summary>
/// Kintoneアプリのスキーマ情報からフィールドコードとC#プロパティ名のマッピングテーブルを生成するサービス実装
/// </summary>
public class NameTableGenerator : INameTableGenerator {
    /// <summary>
    /// Kintoneアプリのスキーマ情報からフィールドコードとC#プロパティ名のマッピングテーブルを生成します。
    /// </summary>
    /// <param name="schema">Kintoneアプリのスキーマ情報</param>
    /// <returns>フィールドコードとC#プロパティ名のマッピングテーブル</returns>
    public NameTable Generate(KintoneAppSchema schema) {
        var table = new NameTable();

        // --- 通常フィールド ---
        foreach (var field in schema.Fields) {
            var mapping = new FieldNameMapping {
                Label = field.Label,
                FieldType = field.FieldType,
                SubTable = null,
                Property = null,
            };

            table[field.FieldCode] = mapping;
        }

        // --- サブテーブルフィールド ---
        foreach (var sub in schema.SubTables) {
            var subMapping = new FieldNameMapping {
                Label = sub.Label,
                FieldType = KintoneFieldType.SubTable,
                SubTable = null,
                Property = null,
            };
            table[sub.FieldCode] = subMapping;

            foreach (var field in sub.Fields) {
                var mapping = new FieldNameMapping {
                    Label = field.Label,
                    FieldType = field.FieldType,
                    SubTable = sub.FieldCode,
                    Property = null,
                };

                table[field.FieldCode] = mapping;
            }
        }

        return table;
    }
}
