using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Extensions;

/// <summary>
/// ILogger拡張メソッド
/// </summary>
public static class ILoggerExtensions {
    private static readonly Action<ILogger, string, Exception?> _logInfoJson =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(4001), "{Json}");
    private static readonly Action<ILogger, string, Exception?> _logWarnJson =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4002), "{Json}");
    private static readonly Action<ILogger, string, Exception?> _logErrorJson =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4003), "{Json}");

    /// <summary>
    /// 情報レベルでオブジェクトをJSON形式でログ出力する拡張メソッド
    /// </summary>
    /// <typeparam name="T">JSONシリアライズ可能なオブジェクトの型</typeparam>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="obj">ログ出力するオブジェクト</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogInformationJson<T>(this ILogger logger, T obj, bool indented = false) where T : IJsonSerializable {
        if (!logger.IsEnabled(LogLevel.Information)) { return; }
        _logInfoJson(logger, obj.ToJsonSafe(indented), null);
    }

    /// <summary>
    /// 警告レベルでオブジェクトをJSON形式でログ出力する拡張メソッド
    /// </summary>
    /// <typeparam name="T">JSONシリアライズ可能なオブジェクトの型</typeparam>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="obj">ログ出力するオブジェクト</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogWarningJson<T>(this ILogger logger, T obj, bool indented = false) where T : IJsonSerializable {
        if (!logger.IsEnabled(LogLevel.Warning)) { return; }
        _logWarnJson(logger, obj.ToJsonSafe(indented), null);
    }

    /// <summary>
    /// エラーレベルでオブジェクトをJSON形式でログ出力する拡張メソッド
    /// </summary>
    /// <typeparam name="T">JSONシリアライズ可能なオブジェクトの型</typeparam>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="obj">ログ出力するオブジェクト</param>
    /// <param name="ex">関連する例外</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogErrorJson<T>(this ILogger logger, T obj, Exception? ex = null, bool indented = false) where T : IJsonSerializable {
        if (!logger.IsEnabled(LogLevel.Error)) { return; }
        _logErrorJson(logger, obj.ToJsonSafe(indented), ex);
    }

    /// <summary>
    /// KintoneErrorをログ出力する拡張メソッド
    /// </summary>
    /// <param name="logger">ログ出力先のILoggerインスタンス</param>
    /// <param name="error">ログ出力するKintoneErrorオブジェクト</param>
    /// <param name="level">ログレベル</param>
    /// <param name="indented">JSONをインデントして出力するかどうか</param>
    public static void LogKintoneError(this ILogger logger, KintoneError error, LogLevel level = LogLevel.Error, bool indented = false) {
        if (!logger.IsEnabled(level)) { return; }
        switch (level) {
            case LogLevel.Information:
                _logInfoJson(logger, error.ToJson(indented), null);
                break;
            case LogLevel.Warning:
                _logWarnJson(logger, error.ToJson(indented), null);
                break;
            case LogLevel.Error:
                _logErrorJson(logger, error.ToJson(indented), null);
                break;
            default:
#pragma warning disable CA1848, CA2254
                logger.Log(level, "{Json}", error.ToJson(indented));
#pragma warning restore CA1848, CA2254
                break;
        }
    }
}
