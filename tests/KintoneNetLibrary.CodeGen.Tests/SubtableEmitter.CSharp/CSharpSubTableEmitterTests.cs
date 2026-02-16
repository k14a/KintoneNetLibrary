using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.SubTableEmitter.CSharp;

public class CSharpSubTableEmitterTests {
    // --- Fake 実装群 ---
    private class FakeNameConverterFactory : INameConverterFactory {
        public INameConverter Create(GenerateLanguages lang) => new CSharpNameConverter();
    }

    private class FakeTypeMapperFactory : ITypeMapperFactory {
        public ITypeMapper Create(GenerateLanguages lang) => new CSharpTypeMapper();
    }

    private class FakeXmlCommentBuilder : IXmlCommentBuilder {
        public string BuildForField(KintoneFieldSchema field)
            => $"    /// <summary>{field.Label}</summary>";

        public string BuildForSubTable(KintoneSubTableSchema sub)
            => $"/// <summary>{sub.Label}</summary>";
    }

    private class FakeLogger : ILogger<CSharpSubTableEmitter> {
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
    public void EmitSubTable_GeneratesExpectedCode() {
        // Arrange
        var emitter = new CSharpSubTableEmitter(
            new FakeNameConverterFactory(),
            new FakeTypeMapperFactory(),
            new FakeXmlCommentBuilder(),
            new FakeLogger()
        );

        var subTable = new KintoneSubTableSchema {
            FieldCode = "order_items",
            Label = "明細",
            Fields =
            [
                new() { FieldCode = "item_name", Label = "商品名", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "qty", Label = "数量", FieldType = KintoneFieldType.Number }
            ]
        };

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated",
            UseKintoneNetLibrary = false
        };

        // Act
        var result = emitter.EmitSubTable("order_items", subTable, options);

        // Assert: クラス名が正しい
        Assert.Equal("SubTableOrderItems", result.ClassName);

        // Assert: スナップショットでコード全体を検証
        Snapshot.Match(result.Code);
    }

    [Fact]
    public void EmitSubTable_GeneratesUniqueClassNames_WhenDuplicated() {
        // Arrange
        var emitter = new CSharpSubTableEmitter(
            new FakeNameConverterFactory(),
            new FakeTypeMapperFactory(),
            new FakeXmlCommentBuilder(),
            new FakeLogger()
        );

        var subTable = new KintoneSubTableSchema {
            FieldCode = "details",
            Label = "明細",
            Fields = []
        };

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated"
        };

        // Act
        var first = emitter.EmitSubTable("details", subTable, options);
        var second = emitter.EmitSubTable("details", subTable, options);

        // Assert
        Assert.Equal("SubTableDetails", first.ClassName);
        Assert.Equal("SubTableDetails1", second.ClassName);
    }
}
