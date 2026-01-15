using KintoneNetLibrary.CodeGen.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// 名前変換ファクトリインターフェース
/// </summary>
public interface INameConverterFactory {
    /// <summary>
    /// 名前変換の作成
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    INameConverter Create(GenerateLanguages lang);
}