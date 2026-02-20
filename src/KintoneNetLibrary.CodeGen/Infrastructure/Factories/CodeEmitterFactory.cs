using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// コードエミッターファクトリ
/// </summary>
/// <param name="emitters">利用可能なコードエミッターのコレクション</param>
public class CodeEmitterFactory(IEnumerable<ICodeEmitter> emitters) : ICodeEmitterFactory {
    /// <summary>
    /// 言語ごとのコードエミッターの辞書
    /// </summary>
    private readonly Dictionary<GenerateLanguages, ICodeEmitter> _emitters = emitters.ToDictionary(e => e.Language, e => e);

    /// <summary>
    /// コードエミッターの作成
    /// </summary>
    /// <param name="lang">生成する言語</param>
    /// <param name="nameConverter">使用する名前変換</param>
    /// <returns>指定された言語のコードエミッター</returns>
    public ICodeEmitter Create(GenerateLanguages lang, INameConverter nameConverter) => this._emitters[lang];
}
