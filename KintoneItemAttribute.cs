using KintoneNetLibrary.Types;
using System;

namespace KintoneNetLibrary;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class KintoneItemAttribute : Attribute {
    /// <summary>
    /// Kintoneフィールドコード
    /// </summary>
    public string FieldCode { get; set; }
    /// <summary>
    /// 日付型
    /// </summary>
    public KintoneDateTime.DateTimeType FieldType { get; set; }
    /// <summary>
    /// 初期値
    /// </summary>
    public object? InitialValue { get; set; }
    /// <summary>
    /// Kintoneへの登録・更新有無
    /// </summary>
    /// <value>TRUE:Kintoneへ登録・更新を行う<br/>FALSE:登録・更新を行わない</value>
    public bool IsUpload { get; set; }
    /// <summary>
    /// Kintone更新時のキー
    /// </summary>
    public bool IsKey { get; set; }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fieldCode"></param>
    /// <param name="fieldType"></param>
    /// <param name="initialValue"></param>
    /// <param name="isUpload"></param>
    /// <param name="isKey"></param>
    public KintoneItemAttribute(string fieldCode = "", KintoneDateTime.DateTimeType fieldType = KintoneDateTime.DateTimeType.DateTime, object? initialValue = null, bool isUpload = true, bool isKey = false) {
        FieldCode = fieldCode;
        this.FieldType = fieldType;
        this.InitialValue = initialValue;
        this.IsUpload = isUpload;
        this.IsKey = isKey;
    }
}