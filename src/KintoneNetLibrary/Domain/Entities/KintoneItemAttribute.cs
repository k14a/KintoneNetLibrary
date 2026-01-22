using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Constructor
/// </summary>
/// <param name="fieldCode"></param>
/// <param name="initialValue"></param>
/// <param name="isUpload"></param>
/// <param name="isKey"></param>
/// <param name="fieldType"></param>
/// <param name="isToJson"></param>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class KintoneItemAttribute(
    string fieldCode = "",
    KintoneFieldType fieldType = KintoneFieldType.Unknown,
    object? initialValue = null,
    bool isUpload = true,
    bool isKey = false,
    bool isToJson = true) : Attribute {

    /// <summary>
    /// Kintoneフィールドコード
    /// </summary>
    public string FieldCode { get; set; } = fieldCode;
    /// <summary>
    /// 初期値
    /// </summary>
    public object? InitialValue { get; set; } = initialValue;
    /// <summary>
    /// Kintoneへの登録・更新有無
    /// </summary>
    /// <value>TRUE:Kintoneへ登録・更新を行う<br/>FALSE:登録・更新を行わない</value>
    public bool IsUpload { get; set; } = isUpload;
    /// <summary>
    /// Kintone更新時のキー
    /// </summary>
    public bool IsKey { get; set; } = isKey;
    /// <summary>
    /// Kintone側のデータタイプ
    /// </summary>
    public KintoneFieldType FieldType { get; set; } = fieldType;
    /// <summary>
    /// プロパティがサブテーブルかどうか
    /// </summary>
    public bool IsSubTable => this.FieldType == KintoneFieldType.SubTable;
    /// <summary>
    /// プロパティをKintoneのJSON形式にシリアライズするかどうか
    /// </summary>
    public bool IsToJson { get; set; } = isToJson;
}