using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// 名前変換ファクトリクラス
/// </summary>
/// <param name="provider"></param>
public class NameConverterFactory() : INameConverterFactory {

    /// <summary>
    /// 名前変換の作成
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public INameConverter Create(GenerateLanguages lang) {
        return lang switch {
            GenerateLanguages.CSharp => new CSharpNameConverter(),
            GenerateLanguages.Python => new PythonNameConverter(),
            _ => throw new NotSupportedException($"Language not supported: {lang}")
        };

    }
}