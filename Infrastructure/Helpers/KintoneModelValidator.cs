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
    public static void ValidateLinkFields<T>(T model) where T : KintoneModelBase {
        var props = typeof(T).GetProperties();

        foreach (var prop in props) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) {
                continue;
            }

            var value = prop.GetValue(model) as string;
            if (string.IsNullOrWhiteSpace(value)) {
                continue; // 空は許容（必要なら Required チェックと組み合わせ）
            }

            switch (attr.FieldType) {
                case KintoneFieldType.LinkUrl:
                    if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                        throw new InvalidOperationException($"'{prop.Name}' は URL として無効です: {value}");
                    }
                    break;

                case KintoneFieldType.LinkTelephone:
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^\+?[0-9\-() ]+$")) {
                        throw new InvalidOperationException($"'{prop.Name}' は電話番号として無効です: {value}");
                    }
                    break;

                case KintoneFieldType.LinkEmail:
                    if (!value.Contains("@") || value.StartsWith("@") || value.EndsWith("@")) {
                        throw new InvalidOperationException($"'{prop.Name}' はメールアドレスとして無効です: {value}");
                    }
                    break;
            }
        }
    }
    public static void ValidateSubTableProperties<T>() {
        var type = typeof(T);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr != null && attr.IsSubTable) {
                // プロパティが List<T> であることを確認
                if (!IsValidSubTableType(prop.PropertyType)) {
                    throw new InvalidOperationException(
                        $"サブテーブル '{prop.Name}' は List<T> 型で定義する必要があります。現在の型: {prop.PropertyType.FullName}"
                    );
                }
            }
        }
    }
    public static void ValidateFileFields<T>(T model) where T : KintoneModelBase {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null || attr.FieldType != KintoneFieldType.File) {
                continue;
            }

            var value = prop.GetValue(model);
            if (value is null) {
                throw new InvalidOperationException(
                    $"File型フィールド '{prop.Name}' の値が null です。空でも IList<KintoneFile> として初期化してください（例：new List<KintoneFile>()）。"
                );
            }

            if (value is not IList<KintoneFile>) {
                throw new InvalidOperationException(
                    $"File型フィールド '{prop.Name}' は IList<KintoneFile> 型として定義されている必要があります。現在の型: {value.GetType().FullName}"
                );
            }
        }
    }

    private static bool IsValidSubTableType(Type type) {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(List<>);
    }

}
