using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// コードエミッタインターフェース
/// </summary>
public interface ICodeEmitter {
    GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options);
}
