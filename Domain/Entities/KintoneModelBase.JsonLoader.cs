using System.Collections;
using System.Reflection;
using System.Text.Json;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

public abstract partial class KintoneModelBase : KintoneModelHookBase {
    public void LoadFromJsonDictionary(Dictionary<string, JsonElement> fieldMap) {
        // 特殊フィールド '$id' → RecordID にセット
        if (fieldMap.TryGetValue("$id", out var idElement)) {
            if (idElement.TryGetProperty("value", out var idValue)) {
                this.RecordID = idValue.GetString();
            }
        }

        foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();

            var fieldCode = attr?.FieldCode ?? prop.Name;
            if (!fieldMap.TryGetValue(fieldCode, out var fieldElement)) {
                continue;
            }

            bool isSubTable = IsSubTableProperty(prop, attr);
            if (isSubTable) {
                if (!fieldElement.TryGetProperty("value", out var subTableArray)) {
                    continue;
                }

                var listInstance = (IList)Activator.CreateInstance(prop.PropertyType)!;
                var itemType = prop.PropertyType.GetGenericArguments().First();

                foreach (var row in subTableArray.EnumerateArray()) {
                    if (!row.TryGetProperty("value", out var rowValue)) {
                        continue;
                    }

                    var subItem = (KintoneSubTableBase)Activator.CreateInstance(itemType)!;
                    var rowDict = new Dictionary<string, JsonElement>();

                    foreach (var field in rowValue.EnumerateObject()) {
                        rowDict[field.Name] = field.Value;
                    }

                    subItem.LoadFromJsonDictionary(rowDict);
                    listInstance.Add(subItem);
                }

                prop.SetValue(this, listInstance);
                continue;
            }

            // 通常フィールド
            if (!fieldElement.TryGetProperty("value", out var valueElement)) {
                continue;
            }

            var fieldType = attr?.FieldType ?? KintoneFieldType.Unknown;
            var value = KintoneValueConverter.ConvertToCSharp(valueElement, fieldType, prop.PropertyType);

            if (value != null) {
                prop.SetValue(this, value);
            }
        }
    }

    /// <summary>
    /// プロパティがサブテーブルかどうかを判定
    /// </summary>
    private static bool IsSubTableProperty(PropertyInfo prop, KintoneItemAttribute? attr) {
        if (attr != null && attr.FieldType == KintoneFieldType.SubTable) { return true; }

        if ((attr == null || attr.FieldType == KintoneFieldType.Unknown) &&
            prop.PropertyType.IsGenericType &&
            prop.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)) {

            var itemType = prop.PropertyType.GetGenericArguments().First();
            return typeof(KintoneSubTableBase).IsAssignableFrom(itemType);
        }

        return false;
    }

}