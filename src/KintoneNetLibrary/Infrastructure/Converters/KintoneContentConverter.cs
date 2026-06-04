using System.Text.Json;

namespace KintoneNetLibrary.Infrastructure.Converters;

// コメントは日本語で記述
/// <summary>
/// Kintoneのコンテンツ変換を行うユーティリティクラス
/// </summary>
public static class KintoneContentConverter {
    /// <summary>
    /// オブジェクトをJSON文字列に変換する
    /// </summary>
    /// <typeparam name="T">変換するオブジェクトの型</typeparam>
    /// <param name="obj">変換するオブジェクト</param>
    /// <returns>JSON文字列</returns>
    public static string ToJson<T>(T obj) {
        return JsonSerializer.Serialize(obj);
    }

    /// <summary>
    /// JSON文字列をオブジェクトに変換する
    /// </summary>
    /// <typeparam name="T">変換するオブジェクトの型</typeparam>
    /// <param name="json">JSON文字列</param>
    /// <returns>変換されたオブジェクト</returns>
    public static T FromJson<T>(string json) {
        return JsonSerializer.Deserialize<T>(json)!;
    }

    /// <summary>
    /// オブジェクトをDictionaryに変換する
    /// </summary>
    /// <typeparam name="T">変換するオブジェクトの型</typeparam>
    /// <param name="obj">変換するオブジェクト</param>
    /// <returns>変換されたDictionary</returns>
    public static Dictionary<string, object> ToDictionary<T>(T obj) {
        var json = ToJson(obj);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
    }

    /// <summary>
    /// Dictionaryをオブジェクトに変換する
    /// </summary>
    /// <typeparam name="T">変換するオブジェクトの型</typeparam>
    /// <param name="dict">変換するDictionary</param>
    /// <returns>変換されたオブジェクト</returns>
    public static T FromDictionary<T>(Dictionary<string, object> dict) {
        var json = JsonSerializer.Serialize(dict);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}
