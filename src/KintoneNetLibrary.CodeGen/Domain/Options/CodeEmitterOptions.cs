namespace KintoneNetLibrary.CodeGen.Domain.Options;

/// <summary>
/// コードエミッタオプション
/// </summary>
public class CodeEmitterOptions {
    /// <summary>
    /// 名前空間
    /// </summary>
    public string Namespace { get; set; } = "KintoneModels";
    /// <summary>
    /// レコード型を使用するかどうか
    /// </summary>
    public bool UseRecord { get; set; } 
    /// <summary>
    /// 部分クラスを生成するかどうか
    /// </summary>
    public bool UsePartial { get; set; } = true;
    /// <summary>
    /// 列挙型を生成するかどうか
    /// </summary>
    public bool GenerateEnums { get; set; }
    /// <summary>
    /// nullable を有効にするかどうか
    /// </summary>
    public bool NullableEnabled { get; set; } = true;
}
