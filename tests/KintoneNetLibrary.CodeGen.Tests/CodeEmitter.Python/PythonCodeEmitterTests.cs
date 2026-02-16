using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.CodeEmitter.Python;

public class PythonCodeEmitterTests {
    // --- Fake 実装群 ---
    private class FakeNameConverterFactory : INameConverterFactory {
        public INameConverter Create(GenerateLanguages lang) => new PythonNameConverter();
    }

    private class FakeTypeMapperFactory : ITypeMapperFactory {
        public ITypeMapper Create(GenerateLanguages lang) => new PythonTypeMapper();
    }

    private class FakeLogger : ILogger<PythonCodeEmitter> {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter) { }

        private class NullScope : IDisposable {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private class FakeClock : IDateTimeProvider {
        public DateTime Now => new(2024, 1, 1);

        public DateTime UtcNow => new(2024, 1, 1);
    }

    [Fact]
    public void Emit_PythonModel_MatchesSnapshot() {
        // Arrange
        var emitter = new PythonCodeEmitter(
            new FakeNameConverterFactory(),
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
