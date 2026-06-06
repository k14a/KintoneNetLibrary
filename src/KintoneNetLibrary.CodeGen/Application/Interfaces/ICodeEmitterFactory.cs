using KintoneNetLibrary.CodeGen.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// コードエミッターファクトリインターフェース
/// </summary>
public interface ICodeEmitterFactory {
    /// <summary>
    /// コードエミッターの作成
    /// </summary>
    /// <param name="lang">生成するコードの言語</param>
    /// <returns>作成されたコードエミッター</returns>
    ICodeEmitter Create(GenerateLanguages lang);
}