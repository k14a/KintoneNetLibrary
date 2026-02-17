using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// 名前変換ファクトリ
/// </summary>
public class NameConverterFactory() : INameConverterFactory {

    /// <summary>
    /// 名前変換の作成
    /// </summary>
    /// <param name="lang">生成する言語</param>
    /// <returns>指定された言語の名前変換</returns>
    /// <exception cref="NotSupportedException">サポートされていない言語が指定された場合にスローされます</exception>
    public INameConverter Create(GenerateLanguages lang) {
        return lang switch {
            GenerateLanguages.CSharp => new CSharpNameConverter(),
            GenerateLanguages.Python => new PythonNameConverter(),
            _ => throw new NotSupportedException($"Language not supported: {lang}")
        };

    }
}