using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Domain.Options;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.HelperClassEmitter.CSharp;

/// <summary>
/// CSharpHelperClassEmitter の単体テストクラス。
/// </summary>
public class CSharpHelperClassEmitterTests {
    /// <summary>
    /// ILogger<CSharpHelperClassEmitter> のテスト用のフェイク実装。ログはすべて無視される。
    /// </summary>
    private class FakeLogger : ILogger<CSharpHelperClassEmitter> {
        /// <summary>
        /// ロガーのスコープを開始するが、実際には何もしない。すべてのログは無視される。
        /// </summary>
        /// <typeparam name="TState">スコープの状態の型</typeparam>
        /// <param name="state">スコープの状態</param>
        /// <returns>スコープの破棄用オブジェクト</returns>
        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        /// <summary>
        /// 指定されたログレベルが有効かどうかを常に false を返すことで、すべてのログを無視する。
        /// </summary>
        /// <param name="logLevel">ログレベル</param>
        /// <returns>常に false を返す</returns>
        public bool IsEnabled(LogLevel logLevel) => false;

        /// <summary>
        /// ログを記録するが、すべてのログは無視されるため、実際には何もしない。
        /// </summary>
        /// <typeparam name="TState">ログの状態の型</typeparam>
        /// <param name="logLevel">ログレベル</param>
        /// <param name="eventId">イベントID</param>
        /// <param name="state">ログの状態</param>
        /// <param name="exception">例外情報</param>
        /// <param name="formatter">ログメッセージのフォーマッタ</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) { }

        /// <summary>
        /// IDisposable の実装で、何もリソースを解放しないダミーのスコープオブジェクト。すべてのログは無視されるため、スコープも実際には何もしない。
        /// </summary>
        private class NullScope : IDisposable {
            /// <summary>
            /// NullScope のシングルトンインスタンス。すべてのログは無視されるため、スコープも実際には何もしない。
            /// </summary>
            public static readonly NullScope Instance = new();

            /// <summary>
            /// IDisposable の実装で、何もリソースを解放しない。すべてのログは無視されるため、スコープも実際には何もしない。
            /// </summary>
            public void Dispose() { }
        }
    }

    /// <summary>
    /// CSharpHelperClassEmitter の EmitHelperClasses メソッドが、指定されたオプションに基づいてすべての期待されるクラスを生成することをテストする。
    /// </summary>
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
