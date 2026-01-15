using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 型マッパーファクトリインターフェース
/// </summary>
public interface ITypeMapperFactory {
    /// <summary>
    /// 型マッパーの作成
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    ITypeMapper Create(GenerateLanguages lang);
}