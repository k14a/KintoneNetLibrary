using System.Text.Json;

namespace KintoneNetLibrary.Utils;

public static class JsonOptionsUtil {
    public static JsonSerializerOptions Clone(JsonSerializerOptions source, bool writeIndented = false) {
        return new JsonSerializerOptions {
            AllowTrailingCommas = source.AllowTrailingCommas,
            DefaultBufferSize = source.DefaultBufferSize,
            DefaultIgnoreCondition = source.DefaultIgnoreCondition,
            DictionaryKeyPolicy = source.DictionaryKeyPolicy,
            Encoder = source.Encoder,
            IgnoreReadOnlyFields = source.IgnoreReadOnlyFields,
            IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties,
            IncludeFields = source.IncludeFields,
            MaxDepth = source.MaxDepth,
            NumberHandling = source.NumberHandling,
            PropertyNamingPolicy = source.PropertyNamingPolicy,
            PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive,
            ReadCommentHandling = source.ReadCommentHandling,
            ReferenceHandler = source.ReferenceHandler,
            TypeInfoResolver = source.TypeInfoResolver,
            UnknownTypeHandling = source.UnknownTypeHandling,
            WriteIndented = writeIndented
        };
    }
}
