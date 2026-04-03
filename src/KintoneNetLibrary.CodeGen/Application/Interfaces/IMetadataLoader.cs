using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// Kintoneアプリのメタデータをロードするインターフェース
/// </summary>
public interface IMetadataLoader {
    /// <summary>
    /// Kintoneアプリのメタデータをロードします。
    /// </summary>
    /// <param name="fieldsJsonPath">fields.json のパス</param>
    /// <param name="layoutJsonPath">layout.json のパス</param>
    /// <returns>ロードされたKintoneAppMetadata</returns>
    Task<KintoneAppMetadata> LoadAsync(string fieldsJsonPath, string layoutJsonPath);
}