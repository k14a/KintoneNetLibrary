using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.CodeGen.Domain.Models;

public class FieldNameMapping {
    /// <summary>
    /// フィールドラベル（AI が意味を理解するために使用）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// フィールドタイプ（NUMBER, DATE, DROP_DOWN など）
    /// </summary>
    public string Type {
        get => this.FieldType.ToString();
        set => this.FieldType = Enum.Parse<KintoneFieldType>(value, ignoreCase: true);
    }

    /// <summary>
    /// サブテーブルコード（通常フィールドは null）
    /// </summary>
    public string? SubTable { get; set; }

    /// <summary>
    /// プロパティ名（AI が埋める or ユーザーが編集）
    /// </summary>
    public string? Property { get; set; }

    /// <summary>
    /// Kintone のフィールドタイプ（AI が埋める or ユーザーが編集）
    /// </summary>
    [JsonIgnore]
    public KintoneFieldType FieldType { get; set; }
}
