using System.Reflection;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Infrastructure.Helpers;

// コメントは日本語で記述
/// <summary>
/// Kintone モデルのバリデーションを行うヘルパークラス
/// </summary>
internal static class KintoneModelValidator {
    /// <summary>
    /// モデルの構造を検証します
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="model">検証対象のモデル</param>
    /// <param name="errors">検証エラーのリスト</param>
    /// <param name="bulk">一括検証対象のモデルのリスト</param>
    /// <returns>検証結果（エラーがなければtrue、エラーがあればfalse）</returns>
    public static bool TryValidateModelStructure<T>(T model, out List<string> errors, IList<T>? bulk = null)
        where T : KintoneModelBase<T>, new() {
        errors = [];

        try {
            ValidateKeyIntegrity(model);
        } catch (Exception ex) {
            errors.Add(ex.Message);
        }

        try {
            ValidateStructuredFields(model);
        } catch (Exception ex) {
            errors.Add(ex.Message);
        }

        if (bulk != null) {
            try {
                ValidateKeyValueUniqueness(bulk);
            } catch (Exception ex) {
                errors.Add(ex.Message);
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// モデルのキー整合性を検証します
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="model">検証対象のモデル</param>
    /// <exception cref="InvalidOperationException">キー整合性が不正な場合にスローされます</exception>
    public static void ValidateKeyIntegrity<T>(T model) where T : KintoneModelBase<T>, new() {
        // 1. IsKey プロパティの重複チェック
        var keyProps = GetKeyProperties<T>();

        if (keyProps.Count > 1) {
            throw new InvalidOperationException(
                $"モデル '{typeof(T).Name}' には IsKey が複数あります（{string.Join(", ", keyProps.Select(p => p.Name))}）"
            );
        }

        if (keyProps.Count == 0) {
            throw new InvalidOperationException("IsKey 属性付きのプロパティが見つかりません。");
        }

        // 2. 値の未設定チェック
        var keyProp = keyProps[0];
        var value = keyProp.GetValue(model);
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s))) {
            throw new InvalidOperationException(
                $"'{keyProp.Name}' は更新キーですが、値が未設定です。"
            );
        }
    }

    /// <summary>
    /// モデルのキー値の一意性を検証します
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="models">検証対象のモデルのリスト</param>
    /// <exception cref="InvalidOperationException">キー値の重複が存在する場合にスローされます</exception>
    public static void ValidateKeyValueUniqueness<T>(IList<T> models) where T : KintoneModelBase<T>, new() {
        var keyProp = GetKeyProperties<T>().FirstOrDefault();

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

    /// <summary>
    /// リンクフィールドの値を検証します
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="model">検証対象のモデル</param>
    /// <exception cref="InvalidOperationException">リンクフィールドの値が不正な場合にスローされます</exception>
    public static void ValidateLinkFields<T>(T model) where T : KintoneModelBase<T>, new() {
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

    /// <summary>
    /// モデルの構造化フィールドを検証します
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <param name="model">検証対象のモデル</param>
    /// <exception cref="InvalidOperationException">構造化フィールドの値が不正な場合にスローされます</exception>
    public static void ValidateStructuredFields<T>(T model) where T : KintoneModelBase<T>, new() {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) { continue; }

            var value = prop.GetValue(model);

            switch (attr.FieldType) {
                case KintoneFieldType.File:
                    if (value == null) {
                        throw new InvalidOperationException($"File型フィールド '{prop.Name}' の値が null です。空でも IList<KintoneFile> として初期化してください。");
                    }

                    if (value is not IList<KintoneFile>) {
                        throw new InvalidOperationException($"File型フィールド '{prop.Name}' は IList<KintoneFile> 型として定義してください。現在の型: {value.GetType().FullName}");
                    }
                    break;

                case KintoneFieldType.CheckBox:
                case KintoneFieldType.MultiSelect:
                case KintoneFieldType.Category:
                    if (value == null) {
                        throw new InvalidOperationException($"複数選択型フィールド '{prop.Name}' の値が null です。空でも IList<string> として初期化してください。");
                    }

                    if (value is not IList<string>) {
                        throw new InvalidOperationException($"フィールド '{prop.Name}' は IList<string> 型として定義してください。現在の型: {value.GetType().FullName}");
                    }
                    break;

                case KintoneFieldType.SubTable:
                    if (!IsValidSubTableType(prop.PropertyType)) {
                        throw new InvalidOperationException($"サブテーブル '{prop.Name}' は List<T> 型で定義する必要があります。現在の型: {prop.PropertyType.FullName}");
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// サブテーブル型のプロパティかどうかを判定します
    /// </summary>
    /// <param name="type">判定対象の型</param>
    /// <returns>サブテーブル型の場合はtrue、それ以外はfalse</returns>
    private static bool IsValidSubTableType(Type type) {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(List<>);
    }

    /// <summary>
    /// モデルのキー属性付きプロパティを取得します
    /// </summary>
    /// <typeparam name="T">検証対象のモデルの型</typeparam>
    /// <returns>キー属性付きプロパティのリスト</returns>
    private static List<PropertyInfo> GetKeyProperties<T>() {
        return [.. typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true)];
    }

}
