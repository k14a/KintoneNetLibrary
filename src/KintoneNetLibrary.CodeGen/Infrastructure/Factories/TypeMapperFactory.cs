using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// 型マッパーファクトリ
/// </summary>
public class TypeMapperFactory() : ITypeMapperFactory {
    /// <summary>
    /// 型マッパーの作成
    /// </summary>
    /// <param name="lang">生成する言語</param>
    /// <returns>指定された言語の型マッパー</returns>
    /// <exception cref="NotSupportedException">サポートされていない言語が指定された場合にスローされます</exception>
    public ITypeMapper Create(GenerateLanguages lang) {
        return lang switch {
            GenerateLanguages.CSharp => new CSharpTypeMapper(),
            GenerateLanguages.Python => new PythonTypeMapper(),
            _ => throw new NotSupportedException($"Language not supported: {lang}")
        };
    }
}