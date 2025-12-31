namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface INameConverter {
    string ToClassName(string label, string code);
    string ToPropertyName(string label, string code);
}

