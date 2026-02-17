using System.Text.Json;

namespace KintoneNetLibrary.Utils;

// コメントは日本語で記述
/// <summary>
/// JsonSerializerOptionsのユーティリティクラス
/// </summary>
public static class JsonOptionsUtil {
    /// <summary>
    /// JsonSerializerOptionsをクローンする
    /// </summary>
    /// <param name="source">クローン元のJsonSerializerOptions</param>
    /// <param name="writeIndented">インデントを有効にするかどうか</param>
    /// <returns>クローンされたJsonSerializerOptions</returns>
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
            WriteIndented = writeIndented,
            // NOTE: 新しいプロパティが追加された場合はここに追記すること
        };
    }
}
