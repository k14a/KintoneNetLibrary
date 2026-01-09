using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface ISubTableEmitter {
    GeneratedSubTableModel EmitSubTable(string name, KintoneSubTableSchema subTable, CodeEmitterOptions options);
}
