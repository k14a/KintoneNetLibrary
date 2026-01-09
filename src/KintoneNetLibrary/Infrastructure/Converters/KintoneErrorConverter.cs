using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

// コメントは日本語で記述
/// <summary>
/// KintoneのエラーレスポンスをKintoneErrorオブジェクトに変換するコンバーター
/// </summary>
public static class KintoneErrorConverter {
    /// <summary>
    /// JSON文字列をKintoneErrorオブジェクトに変換する
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="KintoneException"></exception>
    public static KintoneError Parse(string json) {
        try {
            var error = JsonSerializer.Deserialize<KintoneError>(json) ?? throw new KintoneException("Error response is null.");
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
