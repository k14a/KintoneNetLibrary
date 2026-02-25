using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// コードエミッタインターフェース
/// </summary>
public interface ICodeEmitter {
    /// <summary>
    /// 対応する生成言語
    /// </summary>
    GenerateLanguages Language { get; }

    void SetNameConverter(INameConverter converter);

    /// <summary>
    /// コード生成を実行します
    /// </summary>
    /// <param name="schema">Kintone アプリスキーマ</param>
    /// <param name="options">コード生成オプション</param>
    /// <returns>生成されたモデル結果</returns>
    GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options);
}
