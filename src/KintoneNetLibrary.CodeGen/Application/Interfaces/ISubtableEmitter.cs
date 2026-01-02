using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface ISubtableEmitter {
    GeneratedSubtableModel EmitSubtable(KintoneSubtableSchema subtable, CodeEmitterOptions options);
}
