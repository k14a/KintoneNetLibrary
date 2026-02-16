using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Models;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.HelperClassEmitter.CSharp;

public class CSharpHelperClassEmitterTests {
    private class FakeLogger : ILogger<CSharpHelperClassEmitter> {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter) { }

        private class NullScope : IDisposable {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public void EmitHelperClasses_GeneratesAllExpectedClasses() {
        // Arrange
        var emitter = new CSharpHelperClassEmitter(new FakeLogger());

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated"
        };

        // Act
        var result = emitter.EmitHelperClasses(options).ToList();

        // Assert: 5 クラス生成される
        Assert.Equal(5, result.Count);

        // Assert: スナップショットで内容を検証
        Snapshot.Match(result);
    }
}
