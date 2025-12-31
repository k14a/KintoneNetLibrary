using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Extensions;

public static class ILoggerExtensions {
    public static void LogJson<T>(this ILogger logger, LogLevel level, T obj, bool indented = false, Exception? exception = null) where T : IJsonSerializable {
        var json = obj.ToJsonSafe(indented);
        logger.Log(level, exception, json);
    }

    // よく使うレベルのショートカットも追加可能
    public static void LogInformationJson<T>(this ILogger logger, T obj, bool indented = false) where T : IJsonSerializable
        => logger.LogJson(LogLevel.Information, obj, indented);

    public static void LogWarningJson<T>(this ILogger logger, T obj, bool indented = false) where T : IJsonSerializable
        => logger.LogJson(LogLevel.Warning, obj, indented);

    public static void LogErrorJson<T>(this ILogger logger, T obj, Exception? ex = null, bool indented = false) where T : IJsonSerializable
        => logger.LogJson(LogLevel.Error, obj, indented, ex);

    public static void LogKintoneError(this ILogger logger, KintoneError error, LogLevel level = LogLevel.Error, bool indented = false)
        => logger.Log(level, error.ToJson(indented));

}
