using KintoneNetLibrary.Types;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Model;

/// <summary>
/// Kintone アプリのレコードに対応する抽象基底モデル。
/// 継承して具体モデル（例: BookModel）を作成してください。
/// </summary>
public abstract partial class KintoneModelBase {
    /* ----------  派生クラスが必ず実装するアプリ ID  ---------- */

    /// <summary>このモデルが属する kintone アプリ ID</summary>
    public abstract int AppID { get; }

    /* ----------  共通フィールド  ---------- */

    /// <summary>レコード番号</summary>
    public virtual string? RecordID { get; set; }
    [JsonIgnore()]
    public string? ID {
        get => this.RecordID;
        set => this.RecordID = value;
    }

    /// <summary>登録日時</summary>
    [KintoneItem(FieldType = KintoneDateTime.DateTimeType.DateTime, IsUpload = false)]
    public virtual DateTime CreatedTime { get; set; } = DateTime.MinValue;

    /// <summary>更新日時</summary>
    [KintoneItem(FieldType = KintoneDateTime.DateTimeType.DateTime, IsUpload = false)]
    public virtual DateTime UpdatedTime { get; set; } = DateTime.MinValue;

    /// <summary>作成者</summary>
    public virtual KintoneUser CreatedBy { get; set; } = new();

    /// <summary>更新者</summary>
    public virtual KintoneUser UpdatedBy { get; set; } = new();

    /// <summary>ステータス</summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>作業者</summary>
    public virtual KintoneUser Assignee { get; set; } = new();

    /// <summary>リビジョン番号（初期 -1 : 無視）</summary>
    public virtual int Revision { get; set; } = -1;

    /* ----------  接続オプション（任意） ---------- */

    public virtual string Domain { get; set; } = string.Empty;
    public virtual string ApiToken { get; set; } = string.Empty;
    public virtual string Proxy { get; set; } = string.Empty;
    public virtual string ProxyUser { get; set; } = string.Empty;
    public virtual string ProxyPassword { get; set; } = string.Empty;

    /// <summary>更新時にリビジョンを無視するか (default: false)</summary>
    public bool IgnoreRevision { get; set; }

    /* ----------  日本語項目名 ↔ プロパティ名 変換ルール ---------- */

    // 既定の変換リスト（必要に応じて派生クラスで Override 可能）
    private IList<NameConvertor> _convertDictionary =
    [
        NameConvertor.Create("$id",           nameof(RecordID), NameConvertor.Direction.Read),
        NameConvertor.Create("レコード番号",   nameof(RecordID), NameConvertor.Direction.Send),
        NameConvertor.Create("$revision",     nameof(Revision), NameConvertor.Direction.Read),
        NameConvertor.Create("作成日時",       nameof(CreatedTime)),
        NameConvertor.Create("更新日時",       nameof(UpdatedTime)),
        NameConvertor.Create("作成者",         nameof(CreatedBy)),
        NameConvertor.Create("更新者",         nameof(UpdatedBy)),
        NameConvertor.Create("ステータス",     nameof(Status)),
        NameConvertor.Create("作業者",         nameof(Assignee))
    ];

    /// <summary>
    /// デフォルトの変換辞書を取得・設定。
    /// アプリ側で項目名を変更している場合は派生クラスで上書きしてください。
    /// </summary>
    protected virtual IList<NameConvertor> ConvertDictionary {
        get { return _convertDictionary; }
        set { _convertDictionary = value; }
    }

    /* ----------  コンストラクタ ---------- */

    protected KintoneModelBase() { }

    /* ----------  変換用ディクショナリ取得ヘルパー ---------- */

    public IDictionary<string, string> GetToPropertyDic() {
        return GetNameConvertDic(NameConvertor.Direction.Read);
    }

    public IDictionary<string, string> GetToItemNameDic() {
        return GetNameConvertDic(NameConvertor.Direction.Send);
    }

    private IDictionary<string, string> GetNameConvertDic(NameConvertor.Direction direction) {
        var converts = ConvertDictionary
            .Where(c => c.ConvertDirection == direction || c.ConvertDirection == NameConvertor.Direction.Both);

        return direction == NameConvertor.Direction.Read
            ? converts.ToDictionary(c => c.ItemName, c => c.PropertyName)
            : converts.ToDictionary(c => c.PropertyName, c => c.ItemName);
    }

    public virtual IDictionary<string, object> ToKintoneRecord() {
        var dict = new Dictionary<string, object>();
        var props = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props) {
            if (!prop.CanRead || prop.GetMethod == null) {
                continue;
            }

            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();

            // アップロード対象でなければスキップ
            if (attr != null && !attr.IsUpload) {
                continue;
            }

            var fieldCode = attr?.FieldCode;
            if (string.IsNullOrEmpty(fieldCode)) {
                fieldCode = prop.Name;
            }

            var value = prop.GetValue(this);
            dict[fieldCode] = new { value };
        }

        return dict;
    }
}
