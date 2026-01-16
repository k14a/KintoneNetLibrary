using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Domain.Schemas;

/// <summary>
/// Kintone フィールドスキーマ
/// </summary>
public class KintoneFieldSchema {
    /// <summary>
    /// フィールドコード（例: "customer_name"）
    /// </summary>
    public string FieldCode { get; set; } = "";

    /// <summary>
    /// 表示名（例: "顧客名"）
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Kintone のフィールドタイプ
    /// </summary>
    public KintoneFieldType FieldType { get; set; }

    /// <summary>
    /// 小数点桁数（Number の場合のみ使用）
    /// </summary>
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// 選択肢（CheckBox / Radio / DropDown / MultiSelect）
    /// </summary>
    public List<string> Options { get; set; } = [];

    /// <summary>
    /// 必須フィールドかどうか
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// サブテーブルの場合は true
    /// </summary>
    public bool IsSubTable => this.FieldType == KintoneFieldType.SubTable;
}

