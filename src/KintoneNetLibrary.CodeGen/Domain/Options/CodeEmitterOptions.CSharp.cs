namespace KintoneNetLibrary.CodeGen.Domain.Options;

/// <summary>
/// C# コード生成オプション
/// </summary>
public class CSharpEmitterOptions : CodeEmitterOptions {
    /// <summary>
    /// 生成するクラスの名前空間
    /// </summary>
    public string Namespace { get; set; } = "KintoneModels";

    /// <summary>
    /// record を使用するか（false の場合は class）
    /// </summary>
    public bool UseRecord { get; set; } = false;

    /// <summary>
    /// partial class を生成するか
    /// </summary>
    public bool UsePartial { get; set; } = true;

    /// <summary>
    /// #nullable enable を付与するか
    /// </summary>
    public bool NullableEnabled { get; set; } = true;

    /// <summary>
    /// KintoneNetLibrary を使用するか（false の場合 pure モード）
    /// </summary>
    public bool UseKintoneNetLibrary { get; set; } = true;

}