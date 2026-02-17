using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

/// <summary>
/// Kintoneのシステムフィールドを判定するサービス
/// </summary>
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

    /// <summary>
    /// 指定されたフィールドがKintoneのシステムフィールドかどうかを判定します。
    /// </summary>
    /// <param name="field">判定するフィールドスキーマ</param>
    /// <returns>システムフィールドであればtrue、そうでなければfalse</returns>
    public static bool IsSystemField(KintoneFieldSchema field) => SystemFieldTypes.Contains(field.FieldType);
}
