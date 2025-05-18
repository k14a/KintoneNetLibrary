using KintoneNetLibrary.Types;
using System;

namespace KintoneNetLibrary;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class KintoneItemAttribute : Attribute
{
    public string Code { get; set; }
    public KintoneDateTime.DateTimeType FieldType { get; set; }
    public object? InitialValue { get; set; }
    public bool IsUpload { get; set; }
    public bool IsKey { get; set; }
    public KintoneItemAttribute(string code = "", KintoneDateTime.DateTimeType fieldType = KintoneDateTime.DateTimeType.DateTime, object? initialValue = null, bool isUpload = false, bool isKey = false) {
        Code = code;
        this.FieldType = fieldType;
        this.InitialValue = initialValue;
        this.IsUpload = isUpload;
        this.IsKey = isKey;
    }
}