using System.Reflection;

namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase : KintoneModelHookBase {
    // ----- レコード生成処理 -----
    /// <summary>
    /// Kintoneに送信するためのJSONレコード形式に変換する
    /// </summary>
    /// <returns>フィールドコードをキー、value形式の辞書</returns>
    public Dictionary<string, object> ToKintoneRecord() {
        var record = new Dictionary<string, object>();

        foreach (var prop in GetType().GetProperties()) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) {
                continue;
            }

            // サブテーブルは必ず送信対象（部分更新不可のため丸ごと更新）
            if (attr.FieldType == KintoneFieldType.SubTable) {
                var value = prop.GetValue(this);
                if (value is IEnumerable<KintoneSubTableBase> subTableItems) {
                    var subTableArray = subTableItems.Select(item => {
                        var valueDict = new Dictionary<string, object>();
                        foreach (var itemProp in item.GetType().GetProperties()) {
                            var itemAttr = itemProp.GetCustomAttribute<KintoneItemAttribute>();
                            if (itemAttr == null || !itemAttr.IsUpload) {
                                continue;
                            }

                            var itemValue = itemProp.GetValue(item);
                            object? fieldValue;
                            if (itemValue is IKintoneFieldConverter converter) {
                                fieldValue = converter.ToJson();
                            } else {
                                fieldValue = itemValue;
                            }
                            valueDict[itemAttr.FieldCode] = new { value = fieldValue };
                        }

                        return new {
                            id = item.ID ?? "",  // IDがnullなら空文字列
                            value = valueDict
                        };
                    }).ToList();

                    record[attr.FieldCode] = new { value = subTableArray };
                }
                continue;
            }

            // サブテーブル以外はIsUpload == trueのみ送信
            if (!attr.IsUpload) {
                continue;
            }

            var fieldValueObj = prop.GetValue(this);
            object? fieldValueFinal;
            if (fieldValueObj is IKintoneFieldConverter conv) {
                fieldValueFinal = conv.ToJson();
            } else {
                fieldValueFinal = fieldValueObj;
            }
            record[attr.FieldCode] = new { value = fieldValueFinal };
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
        var keyProp = GetType().GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        if (keyProp is not null) {
            var attr = keyProp.GetCustomAttribute<KintoneItemAttribute>();
            keyValue = keyProp.GetValue(this);
            return (attr?.FieldCode, keyValue);
        }

        keyValue = null;
        return (null, null);
    }

}