using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone アプリのレコードに対応する抽象基底モデル。
/// 継承して具体モデル（例: BookModel）を作成してください。
/// </summary>
public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    /// <summary>
    /// 接続情報（APIトークン・パスワード認証など）
    /// <remarks>アクセスの種類によりApiTokenAccessまたはUserPasswordAccessクラスを使用してください。</remarks>
    /// </summary>
    [KintoneItem(isUpload: false, isToJson: false)]
    public abstract KintoneAccessBase Access { get; init; }

    /// <summary>
    /// Kintoneアカウント情報の取得
    /// <remarks>Kintoneアカウント情報は、APIトークンアクセスやユーザーパスワードアクセスの情報を基に生成されます。</remarks>
    /// </summary>
    [KintoneItem(isUpload: false, isToJson: false)]
    public KintoneAccount Account => this.Access.ToKintoneAccount();

    /// <summary>
    /// KintoneアプリのId
    /// <remarks>アプリの一意な識別子として使用されます。</remarks>
    /// </summary>
    public abstract int AppId { get; init; }

    /// <summary>
    /// レコードの一意な識別子(Record Id)
    /// <remarks>Record IdはKintoneでレコードを一意に識別するためのIdです。</remarks>
    /// </summary>
    [JsonPropertyName("$id")]
    [KintoneItem(fieldCode: "RecordId", isUpload: false)]
    public virtual string? RecordId { get; set; }

    /// <summary>
    /// RecordIdのエイリアスとしてIdプロパティを使用
    /// <remarks>IdプロパティはRecordIdと同じ値を持ちます。</remarks>
    /// </summary>
    [JsonIgnore]
    public string? Id {
        get => this.RecordId;
        set => this.RecordId = value;
    }

    /// <summary>
    /// レコードの作成日時
    /// </summary>
    /// <remarks>KintoneDateTimeクラスを使用して、日時情報を取得できます。</remarks>
    [KintoneItem(fieldType: KintoneFieldType.DateTime, isUpload: false)]
    public virtual KintoneDateTime CreatedTime { get; set; } = new();

    /// <summary>
    /// レコードの更新日時
    /// </summary>
    /// <remarks>KintoneDateTimeクラスを使用して、日時情報を取得できます。</remarks>
    [KintoneItem(fieldType: KintoneFieldType.DateTime, isUpload: false)]
    public virtual KintoneDateTime UpdatedTime { get; set; } = new();

    /// <summary>
    /// レコードの作成者
    /// </summary>
    /// <remarks>KintoneUserクラスを使用して、ユーザー情報を取得できます。</remarks>
    public virtual KintoneUser CreatedBy { get; set; } = new();

    /// <summary>
    /// レコードの更新者
    /// <remarks>KintoneUserクラスを使用して、ユーザー情報を取得できます。</remarks>
    /// </summary>
    public virtual KintoneUser UpdatedBy { get; set; } = new();

    /// <summary>
    /// レコードのステータス
    /// <remarks>ステータスはKintoneアプリのワークフローに関連する情報です。</remarks>
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// レコードのステータス変更日時
    /// <remarks>KintoneDateTimeクラスを使用して、日時情報を取得できます。</remarks>
    /// </summary>
    public virtual KintoneUser Assignee { get; set; } = new();

    /// <summary>
    /// レコードのステータス変更日時
    /// <remarks>KintoneDateTimeクラスを使用して、日時情報を取得できます。</remarks>
    /// </summary>
    public virtual int Revision { get; set; } = -1;

    /// <summary>
    /// レコードの更新時にリビジョンを無視するかどうか
    /// <remarks>リビジョンを無視する場合、更新時にリビジョン番号を送信しません。</remarks>
    /// </summary>
    public bool IgnoreRevision { get; set; }

    /// <summary>
    /// レコードの表示URLを取得します
    /// <remarks>レコードのURLは、Kintoneアカウントのドメイン、アプリId、およびレコードIdを組み合わせて構築されます。</remarks>
    /// </summary>
    public string ShowUrl => $"https://{this.Account.Domain}/k/{this.AppId}/show#record={this.RecordId}";

    /// <summary>
    /// Kintoneモデルのプロパティ名とアイテム名の変換情報
    /// <remarks>プロパティ名とアイテム名の変換は、Kintoneのフィールドコードと一致させるために使用されます。</remarks>
    /// </summary>
    /// <returns>変換情報のリスト</returns>
    public IDictionary<string, string> GetToPropertyDic() => this.GetNameConvertDic(NameConverter.Direction.Read);

    /// <summary>
    /// Kintoneモデルのアイテム名とプロパティ名の変換情報
    /// <remarks>アイテム名とプロパティ名の変換は、Kintoneのフィールドコードと一致させるために使用されます。</remarks>
    /// </summary>
    /// <returns>変換情報のリスト</returns>
    public IDictionary<string, string> GetToItemNameDic() => this.GetNameConvertDic(NameConverter.Direction.Send);

    /// <summary>
    /// Kintoneモデルのプロパティ名とアイテム名の変換情報を取得します。
    /// <remarks>変換情報は、Kintoneのフィールドコードと一致させるために使用されます。</remarks>
    /// </summary>
    /// <param name="direction">変換の方向（読み取りまたは送信）</param>
    /// <returns>変換情報の辞書</returns>
    private Dictionary<string, string> GetNameConvertDic(NameConverter.Direction direction) {
        return this.ConvertDictionary
            .Where(c => c.ConvertDirection == direction || c.ConvertDirection == NameConverter.Direction.Both)
            .ToDictionary(
                c => direction == NameConverter.Direction.Read ? c.ItemName : c.PropertyName,
                c => direction == NameConverter.Direction.Read ? c.PropertyName : c.ItemName
            );
    }
}
