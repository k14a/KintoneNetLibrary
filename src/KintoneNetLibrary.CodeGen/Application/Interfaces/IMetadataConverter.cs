using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// KintoneアプリのメタデータをKintoneAppSchemaに変換するためのインターフェース
/// </summary>
public interface IMetadataConverter {
    /// <summary>
    /// KintoneAppMetadataをKintoneAppSchemaに変換します。
    /// </summary>
    /// <param name="metadata">変換対象のKintoneAppMetadata</param>
    /// <returns>変換後のKintoneAppSchema</returns>
    KintoneAppSchema Convert(KintoneAppMetadata metadata);
}
