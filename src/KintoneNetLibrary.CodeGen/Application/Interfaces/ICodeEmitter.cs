using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// コードエミッタインターフェース
/// </summary>
public interface ICodeEmitter {
    /// <summary>
    /// コードを生成する
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    string Emit(KintoneAppMetadata metadata, CodeEmitterOptions options);
}
