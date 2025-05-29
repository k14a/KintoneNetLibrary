using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone アプリのレコードに対応する抽象基底モデル。
/// 継承して具体モデル（例: BookModel）を作成してください。
/// </summary>
public abstract class KintoneModelBase : KintoneModelHookBase {
    // ----- 必須情報 -----

    /// <summary>Kintone アプリの App ID（必須）</summary>
    public abstract int AppID { get; }

    // ----- 共通フィールド -----

    /// <summary>レコード番号（$id）</summary>
    public virtual string? RecordID { get; set; }

    [JsonIgnore]
    public string? ID {
        get => RecordID;
        set => RecordID = value;
    }

    [KintoneItem(FieldType = KintoneDateTime.DateTimeType.DateTime, IsUpload = false)]
    public virtual DateTime CreatedTime { get; set; } = DateTime.MinValue;

    [KintoneItem(FieldType = KintoneDateTime.DateTimeType.DateTime, IsUpload = false)]
    public virtual DateTime UpdatedTime { get; set; } = DateTime.MinValue;

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

    // ----- 項目名変換 -----

    private IList<NameConvertor> _convertDictionary =
    [
        NameConvertor.Create("$id", nameof(RecordID), NameConvertor.Direction.Read),
        NameConvertor.Create("レコード番号", nameof(RecordID), NameConvertor.Direction.Send),
        NameConvertor.Create("$revision", nameof(Revision), NameConvertor.Direction.Read),
        NameConvertor.Create("作成日時", nameof(CreatedTime)),
        NameConvertor.Create("更新日時", nameof(UpdatedTime)),
        NameConvertor.Create("作成者", nameof(CreatedBy)),
        NameConvertor.Create("更新者", nameof(UpdatedBy)),
        NameConvertor.Create("ステータス", nameof(Status)),
        NameConvertor.Create("作業者", nameof(Assignee))
    ];

    protected virtual IList<NameConvertor> ConvertDictionary {
        get => _convertDictionary;
        set => _convertDictionary = value;
    }

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

    // ----- レコード生成処理 -----
    public virtual IDictionary<string, object> ToKintoneRecord() {
        var record = new Dictionary<string, object>();

        var properties = this.GetType().GetProperties();

        foreach (var prop in properties) {
            var attr = prop.GetCustomAttributes(typeof(KintoneItemAttribute), true)
                           .FirstOrDefault() as KintoneItemAttribute;

            if (attr == null || !attr.IsUpload || string.IsNullOrWhiteSpace(attr.FieldCode)) {
                continue; // Upload 対象ではない、または無効なFieldCode → スキップ
            }

            var value = prop.GetValue(this);
            record[attr.FieldCode] = new { value };
        }

        return record;
    }
    public virtual IDictionary<string, object> ToKintoneUpdateRecord() {
        var record = ToKintoneRecord();

        if (!string.IsNullOrEmpty(ID)) {
            record["id"] = ID;
        } else {
            var (fieldCode, keyValue) = GetUpdateKeyField(out var value);
            if (!string.IsNullOrEmpty(fieldCode) && value is not null) {
                record["updateKey"] = new Dictionary<string, object?> {
                    ["field"] = fieldCode,
                    ["value"] = value
                };
            }
        }

        if (Revision >= 0) {
            record["revision"] = Revision;
        }

        return record;
    }
    private (string? fieldCode, object? value) GetUpdateKeyField(out object? keyValue) {
        var keyProp = GetType().GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        if (keyProp is not null) {
            var attr = keyProp.GetCustomAttribute<KintoneItemAttribute>();
            keyValue = keyProp.GetValue(this);
            return (attr?.FieldCode, keyValue);
        }

        keyValue = null;
        return (null, null);
    }

    // ----- Hook -----
    public virtual async Task RunBeforeCreateHookAsync() => await OnBeforeCreateAsync();
    public virtual async Task RunAfterCreateHookAsync() => await OnAfterCreateAsync();
    public virtual async Task RunBeforeUpdateHookAsync() => await OnBeforeUpdateAsync();
    public virtual async Task RunAfterUpdateHookAsync() => await OnAfterUpdateAsync();
    public virtual async Task RunBeforeDeleteHookAsync() => await OnBeforeDeleteAsync();
    public virtual async Task RunAfterDeleteHookAsync() => await OnAfterDeleteAsync();
    /// <summary>
    /// 登録・更新前に呼び出されるカスタムバリデーション
    /// </summary>
    public virtual void ValidateBeforeSave() { }

    // ----- 判定・ユーティリティ -----
    public virtual bool HasUpdateKeyOrID() {
        if (!string.IsNullOrEmpty(ID)) {
            return true;
        }

        return GetType().GetProperties()
            .Where(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true)
            .Any(p => p.GetValue(this) is string s ? !string.IsNullOrEmpty(s) : p.GetValue(this) is not null);
    }
    protected virtual Task RefreshIdFromKeyAsync() => Task.CompletedTask;
    private KintoneIndex ApplyIndex(KintoneIndexes indexes, int index = 0) {
        if (indexes.IDs.Count > index && indexes.Revisions.Count > index) {
            this.RecordID = indexes.IDs[index];
            this.Revision = Convert.ToInt32(indexes.Revisions[index]);

            return new KintoneIndex {
                ID = RecordID,
                Revision = this.Revision
            };
        }

        return new KintoneIndex();
    }
    public void LoadFromJsonDictionary(Dictionary<string, JsonElement> fieldMap) {
        var props = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) { continue; }

            var fieldCode = attr.FieldCode;
            if (fieldMap.TryGetValue(fieldCode, out var valueElement)) {
                object? value = null;

                if (prop.PropertyType == typeof(string)) {
                    value = valueElement.GetString();
                } else if (prop.PropertyType == typeof(int)) {
                    if (valueElement.ValueKind == JsonValueKind.String && int.TryParse(valueElement.GetString(), out var i)) {
                        value = i;
                    } else if (valueElement.ValueKind == JsonValueKind.Number) {
                        value = valueElement.GetInt32();
                    }
                }
                // 他の型（bool, DateTimeなど）も必要に応じて追加

                if (value != null) {
                    prop.SetValue(this, value);
                }
            }
        }

        // ID / Revision などの標準項目
        if (fieldMap.TryGetValue("$id", out var idElement)) {
            this.ID = idElement.GetString() ?? string.Empty;
        }

        if (fieldMap.TryGetValue("$revision", out var revElement)) {
            if (int.TryParse(revElement.GetString(), out var rev)) {
                this.Revision = rev;
            }
        }
    }

}
