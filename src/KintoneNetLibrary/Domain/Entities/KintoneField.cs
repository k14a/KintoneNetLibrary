using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneのフィールドを表すクラス
/// このクラスは、Kintoneのフィールドのタイプと値を保持します。
/// </summary>
public class KintoneField {
    /// <summary>
    /// フィールドのタイプ
    /// このプロパティは、Kintoneのフィールドのタイプを表します。
    /// 例: "SINGLE_LINE_TEXT", "MULTI_LINE_TEXT", "NUMBER", "CHECK_BOX", "RADIO_BUTTON" など
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// フィールドの値
    /// このプロパティは、Kintoneのフィールドの値を表します。
    /// 値の型はフィールドのタイプによって異なります。
    /// 例: 単一行テキストフィールドの場合は文字列、チェックボックスフィールドの場合は選択された値のリストなど
    /// </summary>
    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;

    /// <summary>
    /// KintoneFieldのコンストラクタ
    /// このコンストラクタは、フィールドのタイプと値を初期化します。
    /// </summary>
    public KintoneField() { }

    /// <summary>
    /// KintoneFieldのコンストラクタ    
    /// このコンストラクタは、フィールドのタイプと値を指定して初期化します。
    /// </summary>
    public KintoneField(string type, object value) {
        this.Type = type;
        this.Value = value;
    }
}