using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface ITypeMapper {
    string Map(KintoneFieldMetadata field);
}
