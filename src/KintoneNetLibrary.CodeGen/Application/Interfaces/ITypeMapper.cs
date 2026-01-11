using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 型マッパーインターフェース
/// </summary>
public interface ITypeMapper {
    /// <summary>
    /// Kintone フィールドを C# 型にマップする
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    string Map(KintoneFieldMetadata field);
    /// <summary>
    /// Kintone フィールドスキーマを C# 型にマップする
    /// </summary>
    /// <param name="field"></param>
    /// <param name="useLibrary"></param>
    /// <param name="subTableClassName"></param>
    /// <returns></returns>
    string MapType(KintoneFieldSchema field, bool useLibrary, string subTableClassName = "");
}
