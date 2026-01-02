using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface IHelperClassEmitter {
    IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options);
}
