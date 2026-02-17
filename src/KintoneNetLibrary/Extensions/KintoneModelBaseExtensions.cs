using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Extensions;

/// <summary>
/// KintoneModelBaseの拡張メソッドを提供するクラス
/// </summary>
public static class KintoneModelBaseExtensions {
    /// <summary>
    /// KintoneModelBaseオブジェクトをKintoneのJSON形式にシリアライズします
    /// </summary>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルの型</typeparam>
    /// <param name="model">シリアライズ対象のモデル</param>
    /// <param name="escapeUnicode">Unicode文字をエスケープするかどうか</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    /// <returns>シリアライズされたJSON文字列</returns>
    public static string ToKintoneJson<T>(this T model, bool escapeUnicode = true, bool indented = false) where T : KintoneModelBase<T>, new() {
        var props = typeof(T)
            .GetProperties()
            .Select(p => new {
                Prop = p,
                Attr = p.GetCustomAttribute<KintoneItemAttribute>()
            })
            .Where(p => p.Attr != null && p.Attr.IsToJson);

        var dict = new Dictionary<string, object?>();

        foreach (var item in props) {
            var value = item.Prop.GetValue(model);
            dict[item.Prop.Name] = value;
        }

        var options = JsonOptionsUtil.Clone(DefaultJsonOptions.Default, writeIndented: indented);
        if (!escapeUnicode) {
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        }
        return JsonSerializer.Serialize(dict, options);
    }
    /// <summary>
    /// KintoneModelBaseオブジェクトのコレクションをKintoneのJSON配列形式にシリアライズします
    /// </summary>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルの型</typeparam>
    /// <param name="models">シリアライズ対象のモデルのコレクション</param>
    /// <param name="escapeUnicode">Unicode文字をエスケープするかどうか</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    /// <returns>シリアライズされたJSON配列文字列</returns>
    public static string ToKintoneJsonArray<T>(this IEnumerable<T> models, bool escapeUnicode = true, bool indented = false) where T : KintoneModelBase<T>, new() {
        var list = models.Select(m => JsonSerializer.Deserialize<Dictionary<string, object?>>(
            m.ToKintoneJson(),
            DefaultJsonOptions.Default
        ));

        var options = JsonOptionsUtil.Clone(DefaultJsonOptions.Default, writeIndented: indented);
        if (!escapeUnicode) {
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        }
        return JsonSerializer.Serialize(list, options);
    }

}
