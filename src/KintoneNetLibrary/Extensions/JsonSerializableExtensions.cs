using System.Text.Json;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Utils;

namespace KintoneNetLibrary.Extensions;

public static class JsonSerializableExtensions {
    private static readonly JsonSerializerOptions _options = DefaultJsonOptions.Default;

    public static string ToJson(this IJsonSerializable obj, bool indented = false) {
        var options = JsonOptionsUtil.Clone(DefaultJsonOptions.Default, indented);
        return JsonSerializer.Serialize(obj, options);
    }
    public static string ToJsonSafe(this IJsonSerializable obj, bool indented = false) {
        try {
            return obj.ToJson(indented);
        } catch (Exception ex) {
            return $"{{\"serializationError\":\"{ex.Message}\"}}";
        }
    }

}
