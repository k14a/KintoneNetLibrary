using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// コードエミッタインターフェース
/// </summary>
public interface ICodeEmitter {
    /// <summary>
    /// コードを生成する
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options);
}
