using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

/// <summary>
/// Kintoneアプリのスキーマ情報から、コード生成に必要なメタデータを変換するサービス実装
/// </summary>
public class MetadataConverter : IMetadataConverter {
    /// <summary>
    /// Kintoneアプリのスキーマ情報から、コード生成に必要なメタデータを変換します。
    /// </summary>
    /// <param name="metadata">変換元のメタデータ</param>
    /// <returns>変換後のスキーマ情報</returns>
    public KintoneAppSchema Convert(KintoneAppMetadata metadata) {
        var layout = metadata.Layout;
        var schema = new KintoneAppSchema {
            AppId = metadata.AppId,
            Revision = metadata.Revision,
            Fields = [],
            SubTables = []
        };

        // 1. fields.json の辞書を作る（高速アクセス用）
        var fieldDict = metadata.Fields.ToDictionary(f => f.FieldCode);

        // 2. layout.json の順序に従ってフィールドを追加
        foreach (var block in layout.Layout) {
            this.AddFieldsFromLayoutBlock(schema, block, fieldDict);
        }

        return schema;
    }

    /// <summary>
    /// layout.json のブロックを再帰的に処理して、schema にフィールドを追加します。
    /// </summary>
    /// <param name="schema">変換先のスキーマ情報</param>
    /// <param name="block">処理するレイアウトブロック</param>
    /// <param name="fieldDict">フィールドコードをキーとしたフィールドメタデータの辞書</param>
    private void AddFieldsFromLayoutBlock(KintoneAppSchema schema, KintoneLayoutBlock block, Dictionary<string, KintoneFieldMetadata> fieldDict) {
        switch (block) {
            case KintoneLayoutRow row:
                foreach (var f in row.Fields) {
                    // LABEL / SPACER / HR は code がない → スキップ
                    if (string.IsNullOrEmpty(f.Code)) { continue; }

                    if (!fieldDict.TryGetValue(f.Code, out var meta)) { continue; }

                    // SUBTABLE は ROW には出てこないはずだが念のため無視
                    if (meta.FieldType == KintoneFieldType.SubTable) { continue; }

                    schema.Fields.Add(this.ConvertField(meta));
                }
                break;

            case KintoneLayoutSubTable sub:
                if (!fieldDict.TryGetValue(sub.Code, out var subMeta)) { return; }

                var subTable = new KintoneSubTableSchema {
                    FieldCode = subMeta.FieldCode,
                    Label = subMeta.FieldLabel,
                    Fields = []
                };

                foreach (var f in sub.Fields) {
                    if (string.IsNullOrEmpty(f.Code)) { continue; }

                    if (!subMeta.SubFields!.Any(sf => sf.FieldCode == f.Code)) { continue; }

                    var sfMeta = subMeta.SubFields!.First(sf => sf.FieldCode == f.Code);
                    subTable.Fields.Add(this.ConvertField(sfMeta));
                }

                schema.SubTables.Add(subTable);
                break;

            case KintoneLayoutGroup group:
                // GROUP は layout[] を再帰的に展開
                foreach (var inner in group.Layout) {
                    this.AddFieldsFromLayoutBlock(schema, inner, fieldDict);
                }
                break;
        }
    }

    /// <summary>
    /// KintoneFieldMetadata を KintoneFieldSchema に変換します。
    /// </summary>
    /// <param name="meta">変換元のフィールドメタデータ</param>
    /// <returns>変換後のフィールドスキーマ</returns>
    private KintoneFieldSchema ConvertField(KintoneFieldMetadata meta) {
        return new KintoneFieldSchema {
            FieldCode = meta.FieldCode,
            Label = meta.FieldLabel,
            FieldType = meta.FieldType,
            Required = meta.Required,
            Options = meta.Options?.ToList() ?? []
        };
    }
}
