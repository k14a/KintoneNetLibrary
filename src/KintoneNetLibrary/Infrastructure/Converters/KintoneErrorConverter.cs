using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

public static class KintoneErrorConverter {
    public static KintoneError Parse(string json) {
        try {
            var error = JsonSerializer.Deserialize<KintoneError>(json);

            if (error == null) {
                throw new KintoneException("Error response is null.");
            }

            if (string.IsNullOrWhiteSpace(error.Message)) {
                // 補足情報を付加して例外化
                throw new KintoneException($"Invalid error structure: missing 'message' field. Raw response: {json}");
            }

            return error;
        } catch (JsonException ex) {
            throw new KintoneException($"Failed to parse error response JSON. Raw response: {json}", ex);
        } catch (Exception ex) when (ex is not KintoneException) {
            throw new KintoneException($"Unexpected error during error response parsing. Raw response: {json}", ex);
        }
    }
}
