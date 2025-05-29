namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Constructor
/// </summary>
/// <param name="fieldCode"></param>
/// <param name="fieldType"></param>
/// <param name="initialValue"></param>
/// <param name="isUpload"></param>
/// <param name="isKey"></param>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class KintoneItemAttribute(string fieldCode = "", KintoneDateTime.DateTimeType fieldType = KintoneDateTime.DateTimeType.DateTime, object? initialValue = null, bool isUpload = true, bool isKey = false) : Attribute {
    /// <summary>
    /// Kintoneフィールドコード
    /// </summary>
    public string FieldCode { get; set; } = fieldCode;
    /// <summary>
    /// 日付型
    /// </summary>
    public KintoneDateTime.DateTimeType FieldType { get; set; } = fieldType;
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
}