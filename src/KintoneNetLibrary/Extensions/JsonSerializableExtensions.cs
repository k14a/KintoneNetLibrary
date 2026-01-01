using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Extensions;

// コメントは日本語で記述
/// <summary>
/// IJsonSerializable インターフェイスを実装するオブジェクトの JSON シリアライズ拡張メソッドを提供します。
/// </summary>
public static class JsonSerializableExtensions {
    /// <summary>
    /// デフォルトの JSON シリアライズオプション
    /// </summary>
    private static readonly JsonSerializerOptions _options = DefaultJsonOptions.Default;

    /// <summary>
    /// オブジェクトを JSON 文字列にシリアライズします。
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="indented"></param>
    /// <returns></returns>
    public static string ToJson(this IJsonSerializable obj, bool indented = false) {
        var options = JsonOptionsUtil.Clone(DefaultJsonOptions.Default, indented);
        return JsonSerializer.Serialize(obj, options);
    }
    /// <summary>
    /// オブジェクトを JSON 文字列にシリアライズします。シリアライズ中に例外が発生した場合、エラーメッセージを含む JSON を返します。     
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="indented"></param>
    /// <returns></returns>
    public static string ToJsonSafe(this IJsonSerializable obj, bool indented = false) {
        try {
            return obj.ToJson(indented);
        } catch (Exception ex) {
            return $"{{\"serializationError\":\"{ex.Message}\"}}";
        }
    }

}
