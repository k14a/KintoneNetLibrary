using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

public class MetadataConverter : IMetadataConverter {
    public KintoneAppSchema Convert(KintoneAppMetadata metadata) {
        var schema = new KintoneAppSchema {
            AppId = metadata.AppId,
            Revision = metadata.Revision,
            // AppName = metadata.AppName ?? string.Empty,
            Fields = [],
            SubTables = []
        };

        foreach (var field in metadata.Fields) {
            if (field.FieldType == KintoneFieldType.SubTable) {
                // --- サブテーブル ---
                var subTable = new KintoneSubTableSchema {
                    FieldCode = field.FieldCode,
                    Label = field.FieldLabel,
                    Fields = [.. (field.SubFields ?? []).Select(sf => new KintoneFieldSchema {
                        FieldCode = sf.FieldCode,
                        Label = sf.FieldLabel,
                        FieldType = sf.FieldType,
                        Required = sf.Required,
                        Options = sf.Options?.ToList() ?? []
                    })]
                };

                schema.SubTables.Add(subTable);
            } else {
                // --- 通常フィールド ---
                schema.Fields.Add(new KintoneFieldSchema {
                    FieldCode = field.FieldCode,
                    Label = field.FieldLabel,
                    FieldType = field.FieldType,
                    Required = field.Required,
                    Options = field.Options?.ToList() ?? []
                });
            }
        }

        return schema;
    }
}
