using System.Text.Json;
using System.Text.Json.Serialization;

namespace KintoneNetLibrary.Domain.Common {
    public static class KintoneJsonOptions {
        public static readonly JsonSerializerOptions Default = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
