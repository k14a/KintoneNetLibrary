using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

public static class KintoneErrorConverter
{
    public static KintoneError Parse(string json) {
        try {
            var error = JsonSerializer.Deserialize<KintoneError>(json);
            if (error != null) {
                return error;
            } else {
                throw new KintoneException("Unknown error.");
            }
        } catch (JsonException ex) {
            throw new KintoneException("Failed to parse error response.", ex);
        }
    }
}
