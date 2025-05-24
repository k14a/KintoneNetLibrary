using System.Text.Json;

namespace KintoneNetLibrary.Infrastructure.Converters;

public static class KintoneContentConverter {
    public static string ToJson<T>(T obj) {
        return JsonSerializer.Serialize(obj);
    }

    public static T FromJson<T>(string json) {
        return JsonSerializer.Deserialize<T>(json);
    }

    public static Dictionary<string, object> ToDictionary<T>(T obj) {
        var json = ToJson(obj);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }

    public static T FromDictionary<T>(Dictionary<string, object> dict) {
        var json = JsonSerializer.Serialize(dict);
        return JsonSerializer.Deserialize<T>(json);
    }
}
