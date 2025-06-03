using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Infrastructure.Converters;

namespace KintoneNetLibrary.Domain.Common;

public static class DefaultJsonOptions {
    public static readonly JsonSerializerOptions Default = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
