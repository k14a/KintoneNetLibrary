using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// 型マッパーファクトリクラス
/// </summary>
/// <param name="provider"></param>
public class TypeMapperFactory() : ITypeMapperFactory {
    /// <summary>
    /// 型マッパーの作成
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public ITypeMapper Create(GenerateLanguages lang) {
        return lang switch {
            GenerateLanguages.CSharp => new CSharpTypeMapper(),
            GenerateLanguages.Python => new PythonTypeMapper(),
            _ => throw new NotSupportedException($"Language not supported: {lang}")
        };
    }
}