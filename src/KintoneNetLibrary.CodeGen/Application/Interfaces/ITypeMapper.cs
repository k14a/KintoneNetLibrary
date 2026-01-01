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
}
