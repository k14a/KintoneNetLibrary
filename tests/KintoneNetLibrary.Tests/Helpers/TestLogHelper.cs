using Microsoft.Extensions.Logging;
using Moq;

namespace KintoneNetLibrary.Tests.Helpers;

public static class TestLogHelper {
    public static void VerifyLog<T>(
    Mock<ILogger<T>> logger,
    LogLevel level,
    string expectedMessage,
    Times times) {
        logger.Verify(l => l.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString().Contains(expectedMessage)),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            times);
    }

}