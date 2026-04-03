using System.Text.Json;

namespace KintoneNetLibrary.Infrastructure.Converters;

/// <summary>
/// Kintone用のJsonSerializerOptionsを提供するクラス
/// </summary>
public static class KintoneJsonOptions {
    private static readonly JsonSerializerOptions _defaultOptions;

    /// <summary>
    /// 静的コンストラクタでデフォルトのJsonSerializerOptionsを初期化
    /// </summary>
    static KintoneJsonOptions() {
        _defaultOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 必要に応じて調整
            WriteIndented = false
        };
        // _defaultOptions.Converters.Add(new KintoneRecordConverterFactory<T>());
        _defaultOptions.Converters.Add(new TimeOnlyJsonConverter());
    }

    /// <summary>
    /// デフォルトのJsonSerializerOptionsを取得
    /// </summary>
    public static JsonSerializerOptions Default => _defaultOptions;

    /// <summary>
    /// カスタムのJsonSerializerOptionsを作成
    /// </summary>
    /// <param name="baseOptions">ベースとなるJsonSerializerOptions</param>
    /// <returns>作成されたJsonSerializerOptions</returns>
    public static JsonSerializerOptions Create(JsonSerializerOptions? baseOptions = null) {
        var options = baseOptions ?? new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        return options;
    }
}
