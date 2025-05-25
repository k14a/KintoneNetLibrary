using System.Reflection;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone アプリのレコードに対応する抽象基底モデル。
/// 継承して具体モデル（例: BookModel）を作成してください。
/// </summary>
public abstract class KintoneModelBase : KintoneModelHookBase {
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

    public virtual async Task RunBeforeCreateHookAsync() => await OnBeforeCreateAsync();
    public virtual async Task RunAfterCreateHookAsync() => await OnAfterCreateAsync();
    public virtual async Task RunBeforeUpdateHookAsync() => await OnBeforeUpdateAsync();
    public virtual async Task RunAfterUpdateHookAsync() => await OnAfterUpdateAsync();
    public virtual async Task RunBeforeDeleteHookAsync() => await OnBeforeDeleteAsync();
    public virtual async Task RunAfterDeleteHookAsync() => await OnAfterDeleteAsync();

    /// <summary>
    /// キー項目から再検索して RecordID を取得（必要に応じて派生クラスで実装）。
    /// </summary>
    protected virtual Task RefreshIdFromKeyAsync() {
        // ここではデフォルト実装を空にしておき、
        // 派生クラスがキー情報を持つ場合は override で ID を埋める。
        return Task.CompletedTask;
    }

    /* ---------- 内部ユーティリティ ---------- */
    /// <summary>
    /// Save / Create / Update の結果 (KintoneIndexes) をモデルへ反映し、単一結果を返す。
    /// </summary>
    private KintoneIndex ApplyIndex(KintoneIndexes indexes, int index = 0) {
        if (indexes.IDs.Count > index && indexes.Revisions.Count > index) {
            this.RecordID = indexes.IDs[index];
            // this.Revision = Convert.ToInt32(indexes.Revisions[index]);
            this.Revision = Convert.ToInt32(indexes.Revisions[index]);

            return new KintoneIndex {
                ID = RecordID,
                Revision = this.Revision,
            };
        }
        return new KintoneIndex();
    }

    public virtual bool HasUpdateKeyOrID() {
        // 1. 明示的な ID がある場合
        if (!string.IsNullOrEmpty(this.ID)) {
            return true;
        }

        // 2. IsKey 属性のあるプロパティが1つ以上セットされている場合
        var keyProps = this.GetType()
            .GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(KintoneItemAttribute)) &&
                        ((KintoneItemAttribute)Attribute.GetCustomAttribute(p, typeof(KintoneItemAttribute))!)!.IsKey);

        foreach (var prop in keyProps) {
            var value = prop.GetValue(this);
            if (value is string str && !string.IsNullOrEmpty(str)) {
                return true;
            }

            if (value != null) {
                return true;
            }
        }

        return false;
    }
    public virtual Dictionary<string, object> ToKintoneUpdateRecord() {
        var record = ToKintoneRecord();

        if (!string.IsNullOrEmpty(ID)) {
            record["id"] = ID;
        } else {
            var keyField = GetUpdateKeyField(out var keyValue);
            if (keyField != null && keyValue != null) {
                record["updateKey"] = new Dictionary<string, object?> {
                    ["field"] = keyField,
                    ["value"] = keyValue
                };
            }
        }

        if (!string.IsNullOrEmpty(Revision)) {
            record["revision"] = Revision;
        }

        return record;
    }
    private (string? fieldCode, object? value) GetUpdateKeyField(out object? keyValue) {
        var keyProp = this.GetType().GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        if (keyProp != null) {
            var attr = keyProp.GetCustomAttribute<KintoneItemAttribute>();
            keyValue = keyProp.GetValue(this);
            return (attr?.FieldCode, keyValue);
        }

        keyValue = null;
        return (null, null);
    }
}
