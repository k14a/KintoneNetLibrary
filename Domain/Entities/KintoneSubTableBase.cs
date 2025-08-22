using System.Reflection;
using System.Text.Json;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Entities;

public abstract class KintoneSubTableBase {
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// サブテーブル行のJSON辞書をプロパティに読み込む
    /// </summary>
    public virtual void LoadFromJsonDictionary(Dictionary<string, JsonElement> fieldMap) {
        foreach (var prop in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) {
                continue;
            }

            if (!fieldMap.TryGetValue(attr.FieldCode, out var fieldElement)) {
                continue;
            }

            if (!fieldElement.TryGetProperty("value", out var valueElement)) {
                continue;
            }

            var fieldType = attr.FieldType;
            var value = KintoneValueConverter.ConvertToCSharp(valueElement, fieldType, prop.PropertyType);

            if (value != null) {
                prop.SetValue(this, value);
            }
        }
    }

}

