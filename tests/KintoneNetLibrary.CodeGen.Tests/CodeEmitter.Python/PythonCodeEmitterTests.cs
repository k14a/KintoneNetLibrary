using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.CodeEmitter.Python;

/// <summary>
/// PythonCodeEmitter の単体テストクラス。
/// </summary>
public class PythonCodeEmitterTests {
    // --- Fake 実装群 ---
    /// <summary>
    /// INameConverterFactory のテスト用のフェイク実装。
    /// </summary>
    private class FakeNameConverterFactory : INameConverterFactory {
        /// <summary>
        /// 指定された言語に対して、PythonNameConverter のインスタンスを返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された名前変換インスタンス</returns>
        public INameConverter Create(GenerateLanguages lang, NameTable? nameTable) => new PythonNameConverter();
    }

    /// <summary>
    /// ITypeMapperFactory のテスト用のフェイク実装。
    /// </summary>
    private class FakeTypeMapperFactory : ITypeMapperFactory {
        /// <summary>
        /// 指定された言語に対して、PythonTypeMapper のインスタンスを返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された型マッパーインスタンス</returns>
        public ITypeMapper Create(GenerateLanguages lang) => new PythonTypeMapper();
    }

    /// <summary>
    /// ILogger<PythonCodeEmitter> のテスト用のフェイク実装。ログはすべて無視される。
    /// </summary>
    private class FakeLogger : ILogger<PythonCodeEmitter> {
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
        /// ログを記録するが、実際には何もしない。すべてのログは無視される。
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
        /// IDisposable の実装で、スコープの開始と終了を管理するためのクラス。実際には何も行わない。
        /// </summary>
        private class NullScope : IDisposable {
            /// <summary>
            /// NullScope のシングルトンインスタンス。すべてのスコープはこのインスタンスを返すことで、実際には何も行わない。
            /// </summary>
            public static readonly NullScope Instance = new();

            /// <summary>
            /// IDisposable の実装で、スコープの終了を管理するが、実際には何も行わない。
            /// </summary>
            public void Dispose() { }
        }
    }

    /// <summary>
    /// IDateTimeProvider のテスト用のフェイク実装。常に固定された日時を返す。
    /// </summary>
    private class FakeClock : IDateTimeProvider {
        /// <summary>
        /// 現在の日時を返すプロパティ。テストの安定性のため、常に 2024-01-01 の日時を返す。
        /// </summary>
        public DateTime Now => new(2024, 1, 1);

        /// <summary>
        /// UTC の現在の日時を返すプロパティ。テストの安定性のため、常に 2024-01-01 の日時を返す。
        /// </summary>
        public DateTime UtcNow => new(2024, 1, 1);
    }

    /// <summary>
    /// PythonCodeEmitter の Emit メソッドが、指定された KintoneAppSchema に対して正しい Python コードを生成するかどうかをテストする。
    /// </summary>
    [Fact]
    public void Emit_PythonModel_MatchesSnapshot() {
        // Arrange
        var emitter = new PythonCodeEmitter(
            new FakeTypeMapperFactory(),
            new FakeClock(),
            new FakeLogger()
        );

        var schema = new KintoneAppSchema {
            AppId = 10,
            AppName = "受注管理",
            Revision = 3,
            Fields = [
                new() { FieldCode = "customer_name", Label = "顧客名", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "order_date", Label = "日付", FieldType = KintoneFieldType.Date },
                new() { FieldCode = "amount", Label = "金額", FieldType = KintoneFieldType.Number },
                new() { FieldCode = "status", Label = "ステータス", FieldType = KintoneFieldType.DropDown, Options = ["新規", "処理中", "完了"] }
            ],
            SubTables = [
                new KintoneSubTableSchema {
                    FieldCode = "items",
                    Label = "明細",
                    Fields = [
                        new() { FieldCode = "item_name", Label = "商品名", FieldType = KintoneFieldType.SingleLineText },
                        new() { FieldCode = "qty", Label = "数量", FieldType = KintoneFieldType.Number },
                        new() { FieldCode = "unit_price", Label = "単価", FieldType = KintoneFieldType.Number }
                    ]
                }
            ]
        };

        var options = new PythonEmitterOptions {
            ModuleName = "order_model",
            UseTypeHint = true,
            HeaderComment = "Generated for testing"
        };

        // Act
        var result = emitter.Emit(schema, options);

        // Assert
        Snapshot.Match(result.MainModelCode);
    }
}
