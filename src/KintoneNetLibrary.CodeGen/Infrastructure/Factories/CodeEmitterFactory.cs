using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// コードエミッターファクトリクラス
/// </summary>
public class CodeEmitterFactory(
    INameConverterFactory converterFactory,
    ITypeMapperFactory mapperFactory,
    IXmlCommentBuilder xml,
    ISubTableEmitter subTableEmitter,
    IHelperClassEmitter helper,
    ILogger<CodeEmitterFactory> logger) : ICodeEmitterFactory {
    private readonly ITypeMapperFactory _mapperFactory = mapperFactory;
    private readonly INameConverterFactory _converterFactory = converterFactory;
    private readonly IXmlCommentBuilder _xml = xml;
    private readonly ISubTableEmitter _subTableEmitter = subTableEmitter;
    private readonly IHelperClassEmitter _helper = helper;
    private readonly ILogger<CodeEmitterFactory> _logger = logger;
    /// <summary>
    /// コードエミッターの作成
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public ICodeEmitter Create(GenerateLanguages lang) {
        return lang switch {
            GenerateLanguages.CSharp => new CSharpCodeEmitter(this._converterFactory, this._mapperFactory,this._xml,subTableEmitter,this._helper),
            GenerateLanguages.Python => new PythonCodeEmitter(this._converterFactory, this._mapperFactory),
            _ => throw new NotSupportedException($"Language not supported: {lang}")
        };
    }
}
