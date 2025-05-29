using System.Reflection;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Helpers;

internal static class KintoneModelValidator {
    public static void ValidateUpdateKey<T>(T model) where T : KintoneModelBase {
        var updateKeyProperty = model.GetType().GetProperty("IsKey") ?? throw new InvalidOperationException($"IsKey 属性が付与されたプロパティが見つかりません。");

        var value = updateKeyProperty.GetValue(model);
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s))) {
            throw new InvalidOperationException($"'{updateKeyProperty.Name}' は IsKey に指定されていますが、値が未設定です。");
        }
    }

    public static void ValidateUniqueKeyProperty<T>(T model) where T : KintoneModelBase {
        var keyProps = model.GetType().GetProperties().Where(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true).ToList();

        if (keyProps.Count > 1) {
            throw new InvalidOperationException($"モデル '{typeof(T).Name}' には IsKey が複数のプロパティに設定されています（{string.Join(", ", keyProps.Select(p => p.Name))}）。1つにしてください。");
        }
    }
    public static void ValidateDuplicateKeys<T>(IList<T> models) where T : KintoneModelBase {
        var keyProp = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        if (keyProp == null) {
            return; // キーなし → チェック不要
        }

        var duplicateKeys = models
            .Where(m => keyProp.GetValue(m) != null)
            .GroupBy(m => keyProp.GetValue(m))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key?.ToString())
            .ToList();

        if (duplicateKeys.Count != 0) {
            throw new InvalidOperationException($"同じキー値が複数存在します: {string.Join(", ", duplicateKeys)}");
        }
    }
}
