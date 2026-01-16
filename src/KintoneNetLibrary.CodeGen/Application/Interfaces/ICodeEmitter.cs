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
    /// <summary>
    /// コード生成を実行します
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options);
}
