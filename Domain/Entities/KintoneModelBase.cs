using System.Reflection;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone アプリのレコードに対応する抽象基底モデル。
/// 継承して具体モデル（例: BookModel）を作成してください。
/// </summary>
public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    // ----- 必須情報 -----

    // protected KintoneAccessBase _access;
    /// <summary>接続情報（APIトークン・パスワード認証など）</summary>
    [JsonIgnore]
    [KintoneItem(isUpload: false)]
    public abstract KintoneAccessBase Access { get; init; }

    /// <summary>Kintoneアカウント情報の取得</summary>
    [JsonIgnore]
    [KintoneItem(isUpload: false)]
    public KintoneAccount Account => this.Access.ToKintoneAccount();

    /// <summary>AppID</summary>
    public abstract int AppID { get; }

    // ----- 共通フィールド -----

    [JsonPropertyName("$id")]
    [KintoneItem(fieldCode: "RecordID", isUpload: false)]
    public virtual string? RecordID { get; set; }

    [JsonIgnore]
    public string? ID {
        get => this.RecordID;
        set => this.RecordID = value;
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

    // ----- Proxy関連（任意） -----

    public virtual string Proxy { get; set; } = string.Empty;
    public virtual string ProxyUser { get; set; } = string.Empty;
    public virtual string ProxyPassword { get; set; } = string.Empty;

    // ----- 動作制御オプション -----

    public bool IgnoreRevision { get; set; }

    // ----- Name変換辞書の取得 -----

    public IDictionary<string, string> GetToPropertyDic() => this.GetNameConvertDic(NameConvertor.Direction.Read);
    public IDictionary<string, string> GetToItemNameDic() => this.GetNameConvertDic(NameConvertor.Direction.Send);

    private IDictionary<string, string> GetNameConvertDic(NameConvertor.Direction direction) {
        return this.ConvertDictionary
            .Where(c => c.ConvertDirection == direction || c.ConvertDirection == NameConvertor.Direction.Both)
            .ToDictionary(
                c => direction == NameConvertor.Direction.Read ? c.ItemName : c.PropertyName,
                c => direction == NameConvertor.Direction.Read ? c.PropertyName : c.ItemName
            );
    }
}
