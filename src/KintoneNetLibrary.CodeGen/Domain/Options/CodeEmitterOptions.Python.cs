namespace KintoneNetLibrary.CodeGen.Domain.Options;

/// <summary>
/// Python コード生成オプション
/// </summary>
public class PythonEmitterOptions : CodeEmitterOptions {
    /// <summary>
    /// 生成するモジュール名
    /// </summary>
    public string ModuleName { get; set; } = "kintone_models";

    /// <summary>
    /// type hint を付与するか
    /// </summary>
    public bool UseTypeHint { get; set; } = true;
}