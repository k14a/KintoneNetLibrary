using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Infrastructure.Converters;

public static class KintoneJsonOptions {
    private static readonly JsonSerializerOptions _defaultOptions;

    static KintoneJsonOptions() {
        _defaultOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 必要に応じて調整
            WriteIndented = false
        };
        // _defaultOptions.Converters.Add(new KintoneRecordConverterFactory<T>());
        _defaultOptions.Converters.Add(new TimeOnlyJsonConverter());
    }

    public static JsonSerializerOptions Default => _defaultOptions;

    public static JsonSerializerOptions Create(JsonSerializerOptions? baseOptions = null) {
        var options = baseOptions ?? new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        // options.Converters.Add(new KintoneRecordConverterFactory<T>());
        return options;
    }
}
