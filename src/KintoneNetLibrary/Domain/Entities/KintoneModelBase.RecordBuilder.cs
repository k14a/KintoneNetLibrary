using System.Reflection;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneモデル基底クラス（レコードビルダー機能）
/// </summary>
public abstract partial class KintoneModelBase<TSelf> : KintoneModelHookBase where TSelf : KintoneModelBase<TSelf>, new() {
    // ----- レコード生成処理 -----
    /// <summary>
    /// Kintoneに送信するためのJSONレコード形式に変換する
    /// </summary>
    /// <returns>フィールドコードをキー、value形式の辞書</returns>
    public Dictionary<string, object> ToKintoneRecord() {
        var record = new Dictionary<string, object>();

        foreach (var prop in this.GetType().GetProperties()) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) {
                continue;
            }

            // サブテーブルはIsUpload == trueの場合のみ送信対象（送信する場合は部分更新不可のため丸ごと更新）
            if (attr.FieldType == KintoneFieldType.SubTable) {
                if (!attr.IsUpload) {
                    continue;
                }

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
                            if (itemValue is KintoneDateTime kd) {
                                fieldValue = kd.ToJson(itemAttr.FieldType);
                            } else if (itemValue is IKintoneFieldConverter converter) {
                                fieldValue = converter.ToJson();
                            } else if (itemValue is null) {
                                fieldValue = null;
                            } else {
                                fieldValue = itemValue;
                            }
                            valueDict[itemAttr.FieldCode] = new { value = fieldValue };
                        }

                        return new {
                            id = item.Id ?? "",  // Idがnullなら空文字列
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

            // 必須項目はクリア不可能なため、IsRequired と ClearIfNull の同時指定は矛盾した属性定義とみなす
            if (attr.IsRequired && attr.ClearIfNull) {
                throw new InvalidOperationException($"フィールド '{attr.FieldCode}' に IsRequired と ClearIfNull を同時に指定することはできません。");
            }

            var fieldValueObj = prop.GetValue(this);
            object? fieldValueFinal;
            if (fieldValueObj is IKintoneFieldConverter conv) {
                fieldValueFinal = conv.ToJson();
            } else {
                fieldValueFinal = fieldValueObj;
            }

            if (fieldValueFinal is null && attr.ClearIfNull) {
                record[attr.FieldCode] = new { value = GetClearValue(attr.FieldType) };
            } else {
                record[attr.FieldCode] = new { value = fieldValueFinal };
            }
        }

        return record;
    }

    /// <summary>
    /// 複数値を持つフィールドタイプの一覧（クリア時に空配列を送信する必要がある）
    /// </summary>
    private static readonly HashSet<KintoneFieldType> MultiValueFieldTypes = [
        KintoneFieldType.CheckBox,
        KintoneFieldType.MultiSelect,
        KintoneFieldType.Category,
        KintoneFieldType.UserSelect,
        KintoneFieldType.OrganizationSelect,
        KintoneFieldType.GroupSelect,
        KintoneFieldType.File,
    ];

    /// <summary>
    /// フィールドタイプに応じたクリア用の値を取得する
    /// </summary>
    /// <param name="fieldType">Kintone側のフィールドタイプ</param>
    /// <returns>複数値フィールドの場合は空配列、それ以外は空文字列</returns>
    private static object GetClearValue(KintoneFieldType fieldType) {
        return MultiValueFieldTypes.Contains(fieldType) ? new List<object>() : "";
    }

    /// <summary>
    /// Kintoneのレコード更新用形式に変換する
    /// </summary>
    /// <returns>更新用レコードの辞書形式</returns>
    public virtual IDictionary<string, object> ToKintoneUpdateRecord() {
        var record = this.ToKintoneRecord();

        if (!string.IsNullOrEmpty(this.Id)) {
            record["id"] = this.Id;
        } else {
            var (fieldCode, keyValue) = this.GetUpdateKeyField(out var value);
            if (!string.IsNullOrEmpty(fieldCode) && value is not null) {
                record["updateKey"] = new Dictionary<string, object?> {
                    ["field"] = fieldCode,
                    ["value"] = value
                };
            }
        }

        if (this.Revision >= 0) {
            record["revision"] = this.Revision;
        }

        return record;
    }
    /// <summary>
    /// 更新キー用フィールドを取得する
    /// </summary>
    /// <param name="keyValue">更新キーの値</param>
    /// <returns>更新キーのフィールドコードと値のタプル</returns>
    private (string? fieldCode, object? value) GetUpdateKeyField(out object? keyValue) {
        var keyProp = this.GetType().GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        if (keyProp is not null) {
            var attr = keyProp.GetCustomAttribute<KintoneItemAttribute>();
            keyValue = keyProp.GetValue(this);
            return (attr?.FieldCode, keyValue);
        }

        keyValue = null;
        return (null, null);
    }

}