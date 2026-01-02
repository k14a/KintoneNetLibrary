namespace KintoneNetLibrary.CodeGen.Domain.Models;

/// <summary>
/// 補助クラス（UserInfo / GroupInfo / OrganizationInfo など）の生成結果
/// </summary>
public class GeneratedHelperClass {
    /// <summary>
    /// 生成されたクラス名（例: UserInfo）
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 生成された C# コード全文
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
