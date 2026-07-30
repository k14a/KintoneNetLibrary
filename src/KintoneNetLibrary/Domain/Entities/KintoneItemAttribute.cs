using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Constructor
/// </summary>
/// <param name="fieldCode"></param>
/// <param name="initialValue"></param>
/// <param name="isUpload"></param>
/// <param name="isDownload"></param>
/// <param name="isKey"></param>
/// <param name="fieldType"></param>
/// <param name="isToJson"></param>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class KintoneItemAttribute(
    string fieldCode = "",
    KintoneFieldType fieldType = KintoneFieldType.Unknown,
    object? initialValue = null,
    bool isUpload = true,
    bool isDownload = true,
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
    /// Kintoneからの読み込み有無
    /// </summary>
    /// <value>TRUE:Kintoneから読み込みを行う<br/>FALSE:読み込みを行わない</value>
    public bool IsDownload { get; set; } = isDownload;
    /// <summary>
    /// Kintone更新時のキー
    /// </summary>
    public bool IsKey { get; set; } = isKey;
    /// <summary>
    /// 必須項目かどうか
    /// </summary>
    public bool IsRequired { get; set; }
    /// <summary>
    /// 値が null の場合に Kintone 側の既存値をクリアするかどうか
    /// </summary>
    /// <value>TRUE:null の場合に空値を送信してクリアする<br/>FALSE:null の場合は送信自体を省略し、Kintone側の既存値を変更しない（デフォルト）</value>
    public bool ClearIfNull { get; set; }
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