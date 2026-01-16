using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// コードエミッターファクトリクラス
/// </summary>
public class CodeEmitterFactory(IEnumerable<ICodeEmitter> emitters) : ICodeEmitterFactory {
    private readonly Dictionary<GenerateLanguages, ICodeEmitter> _emitters = emitters.ToDictionary(e => e.Language, e => e);

    public ICodeEmitter Create(GenerateLanguages lang) => this._emitters[lang];
}
