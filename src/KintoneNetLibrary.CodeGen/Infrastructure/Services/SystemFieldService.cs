using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Domain.Services;

public static class SystemFieldService {
    private static readonly HashSet<KintoneFieldType> SystemFieldTypes = [
        KintoneFieldType.RecordNumber,   // __ID__
        KintoneFieldType.Revision,       // __REVISION__
        KintoneFieldType.Creator,
        KintoneFieldType.CreatedTime,
        KintoneFieldType.Modifier,
        KintoneFieldType.UpdatedTime,
        KintoneFieldType.Status,
        KintoneFieldType.Category,
        KintoneFieldType.Assignee
    ];

    public static bool IsSystemField(KintoneFieldSchema field) => SystemFieldTypes.Contains(field.FieldType);
}
