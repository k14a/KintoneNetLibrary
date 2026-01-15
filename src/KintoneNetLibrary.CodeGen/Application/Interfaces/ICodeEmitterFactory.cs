using KintoneNetLibrary.CodeGen.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// コードエミッターファクトリインターフェース
/// </summary>
public interface ICodeEmitterFactory {
    /// <summary>
    /// コードエミッターの作成
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    ICodeEmitter Create(GenerateLanguages lang);
}