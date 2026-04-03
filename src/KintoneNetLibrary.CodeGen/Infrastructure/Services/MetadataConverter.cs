using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

public class MetadataConverter : IMetadataConverter {
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
