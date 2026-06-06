using Microsoft.Extensions.Logging;
using Moq;

namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// テスト用のログ検証ヘルパークラス。
/// </summary>
public static class TestLogHelper {
    /// <summary>
    /// 指定されたログレベルとメッセージを含むログが、指定された回数だけ記録されたことを検証します。
    /// </summary>
    /// <typeparam name="T">ログのカテゴリタイプ</typeparam>
    /// <param name="logger">検証対象のモックログ</param>
    /// <param name="level">期待されるログレベル</param>
    /// <param name="expectedMessage">期待されるログメッセージ</param>
    /// <param name="times">期待されるログの呼び出し回数</param>
    public static void VerifyLog<T>(
        Mock<ILogger<T>> logger,
        LogLevel level,
        string expectedMessage,
        Times times) {

        logger.Verify(l => l.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(expectedMessage)),
            It.IsAny<Exception?>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            times);
    }
}