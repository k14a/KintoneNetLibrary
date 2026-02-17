using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Extensions;

/// <summary>
/// ILogger拡張メソッド
/// </summary>
public static class ILoggerExtensions {
    /// <summary>
    /// オブジェクトをJSON形式でログ出力する拡張メソッド
    /// </summary>
    /// <typeparam name="T">JSONシリアライズ可能なオブジェクトの型</typeparam>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="level">ログレベル</param>
    /// <param name="obj">ログ出力するオブジェクト</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    /// <param name="exception">関連する例外</param>
    public static void LogJson<T>(this ILogger logger, LogLevel level, T obj, bool indented = false, Exception? exception = null) where T : IJsonSerializable {
        var json = obj.ToJsonSafe(indented);
        logger.Log(level, exception, json);
    }

    // よく使うレベルのショートカットも追加可能
    /// <summary>
    /// 情報レベルでオブジェクトをJSON形式でログ出力する拡張メソッド
    /// </summary>
    /// <typeparam name="T">JSONシリアライズ可能なオブジェクトの型</typeparam>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="obj">ログ出力するオブジェクト</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogInformationJson<T>(this ILogger logger, T obj, bool indented = false) where T : IJsonSerializable
        => logger.LogJson(LogLevel.Information, obj, indented);

    /// <summary>
    /// 警告レベルでオブジェクトをJSON形式でログ出力する拡張メソッド
    /// </summary>
    /// <typeparam name="T">JSONシリアライズ可能なオブジェクトの型</typeparam>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="obj">ログ出力するオブジェクト</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogWarningJson<T>(this ILogger logger, T obj, bool indented = false) where T : IJsonSerializable
        => logger.LogJson(LogLevel.Warning, obj, indented);

    /// <summary>
    /// エラーレベルでオブジェクトをJSON形式でログ出力する拡張メソッド
    /// </summary>
    /// <typeparam name="T">JSONシリアライズ可能なオブジェクトの型</typeparam>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="obj">ログ出力するオブジェクト</param>
    /// <param name="ex">関連する例外</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogErrorJson<T>(this ILogger logger, T obj, Exception? ex = null, bool indented = false) where T : IJsonSerializable
        => logger.LogJson(LogLevel.Error, obj, indented, ex);

    /// <summary>
    /// KintoneErrorをログ出力する拡張メソッド
    /// </summary>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="error">ログ出力するKintoneErrorオブジェクト</param>
    /// <param name="level">ログレベル</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogKintoneError(this ILogger logger, KintoneError error, LogLevel level = LogLevel.Error, bool indented = false)
        => logger.Log(level, error.ToJson(indented));

}
