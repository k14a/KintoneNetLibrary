using KintoneNetLibrary.CodeGen.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 名前変換ファクトリインターフェース
/// </summary>
public interface INameConverterFactory {
    /// <summary>
    /// 名前変換の作成
    /// </summary>
    /// <param name="lang">生成するコードの言語</param>
    /// <returns>作成された名前変換</returns>
    INameConverter Create(GenerateLanguages lang);
}