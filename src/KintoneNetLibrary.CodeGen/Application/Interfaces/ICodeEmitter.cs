using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface ICodeEmitter {
    string Emit(KintoneAppMetadata metadata, CodeEmitterOptions options);
}
