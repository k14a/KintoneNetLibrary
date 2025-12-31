namespace KintoneNetLibrary.CodeGen.Domain.Options;

public class CodeEmitterOptions {
    public string Namespace { get; set; } = "KintoneModels";
    public bool UseRecord { get; set; } 
    public bool UsePartial { get; set; } = true;
    public bool GenerateEnums { get; set; }
    public bool NullableEnabled { get; set; } = true;
}
