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
    /// <param name="lang">生成するコードの言語</param>
    /// <returns>作成された型マッパー</returns>
    ITypeMapper Create(GenerateLanguages lang);
}