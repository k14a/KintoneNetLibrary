using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone アプリのレコードに対応する抽象基底モデル。
/// 継承して具体モデル（例: BookModel）を作成してください。
/// </summary>
public abstract partial class KintoneModelBase : KintoneModelHookBase {
    // ----- 必須情報 -----

    /// <summary>Kintone アプリの App ID（必須）</summary>
    public abstract int AppID { get; }

    // ----- 共通フィールド -----

    /// <summary>レコード番号（$id）</summary>
    [KintoneItem(fieldCode: "レコード番号", isUpload: false)]
    public virtual string? RecordID { get; set; }

    [JsonIgnore]
    public string? ID {
        get => RecordID;
        set => RecordID = value;
    }

    [KintoneItem(fieldType: KintoneFieldType.DateTime, isUpload: false)]
    public virtual KintoneDateTime CreatedTime { get; set; } = new();

    [KintoneItem(fieldType: KintoneFieldType.DateTime, isUpload: false)]
    public virtual KintoneDateTime UpdatedTime { get; set; } = new();

    public virtual KintoneUser CreatedBy { get; set; } = new();
    public virtual KintoneUser UpdatedBy { get; set; } = new();
    public virtual string Status { get; set; } = string.Empty;
    public virtual KintoneUser Assignee { get; set; } = new();
    public virtual int Revision { get; set; } = -1;

    // ----- 接続オプション -----

    public virtual string Domain { get; set; } = string.Empty;
    public virtual string ApiToken { get; set; } = string.Empty;
    public virtual string Proxy { get; set; } = string.Empty;
    public virtual string ProxyUser { get; set; } = string.Empty;
    public virtual string ProxyPassword { get; set; } = string.Empty;
    public bool IgnoreRevision { get; set; }

    public IDictionary<string, string> GetToPropertyDic() => GetNameConvertDic(NameConvertor.Direction.Read);
    public IDictionary<string, string> GetToItemNameDic() => GetNameConvertDic(NameConvertor.Direction.Send);
    private IDictionary<string, string> GetNameConvertDic(NameConvertor.Direction direction) {
        return ConvertDictionary
            .Where(c => c.ConvertDirection == direction || c.ConvertDirection == NameConvertor.Direction.Both)
            .ToDictionary(
                c => direction == NameConvertor.Direction.Read ? c.ItemName : c.PropertyName,
                c => direction == NameConvertor.Direction.Read ? c.PropertyName : c.ItemName
            );
    }

}
