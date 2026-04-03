using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Factories;

/// <summary>
/// 名前変換ファクトリ
/// </summary>
public class NameConverterFactory(IServiceProvider provider) : INameConverterFactory {
    private readonly IServiceProvider _provider = provider;

    /// <summary>
    /// 名前変換の作成
    /// </summary>
    /// <param name="lang">生成する言語</param>
    /// <returns>指定された言語の名前変換</returns>
    /// <exception cref="NotSupportedException">サポートされていない言語が指定された場合にスローされます</exception>
    public INameConverter Create(GenerateLanguages lang, NameTable? nameTable = null) {
        var converter = (INameConverter)(lang switch {
            GenerateLanguages.CSharp => this._provider.GetRequiredService<CSharpNameConverter>(),
            GenerateLanguages.Python => this._provider.GetRequiredService<PythonNameConverter>(),
            GenerateLanguages.TypeScript => throw new NotImplementedException(),
            GenerateLanguages.Go => throw new NotImplementedException(),
            GenerateLanguages.Java => throw new NotImplementedException(),
            _ => throw new NotSupportedException($"Language not supported: {lang}")
        });

        // ★ 変換テーブルが指定されていて、かつ適用可能なら適用する
        if (nameTable != null && converter is INameTableApplicable applicable) {
            applicable.LoadNameTable(nameTable);
        }

        return converter;
    }
}