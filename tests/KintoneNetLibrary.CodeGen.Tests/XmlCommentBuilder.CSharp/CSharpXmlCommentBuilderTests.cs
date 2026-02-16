using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.XmlCommentBuilder.CSharp;

public class CSharpXmlCommentBuilderTests {
    private class FakeLogger : ILogger<CSharpXmlCommentBuilder> {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter) { }

        private class NullScope : IDisposable {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private readonly CSharpXmlCommentBuilder _builder = new(new FakeLogger());

    // -----------------------------
    // BuildForField
    // -----------------------------
    [Fact]
    public void BuildForField_WithLabel_GeneratesSummary() {
        var field = new KintoneFieldSchema {
            Label = "顧客名",
            FieldType = KintoneFieldType.SingleLineText
        };

        var result = _builder.BuildForField(field);

        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// 顧客名", result);
        Assert.Contains("/// </summary>", result);
    }

    [Fact]
    public void BuildForField_WithoutLabel_UsesDefaultSummary() {
        var field = new KintoneFieldSchema {
            Label = "",
            FieldType = KintoneFieldType.SingleLineText
        };

        var result = _builder.BuildForField(field);

        Assert.Contains("/// Kintone field", result);
    }

    [Fact]
    public void BuildForField_CalcField_AddsRemarks() {
        var field = new KintoneFieldSchema {
            Label = "計算結果",
            FieldType = KintoneFieldType.Calc
        };

        var result = _builder.BuildForField(field);

        Assert.Contains("<remarks>", result);
        Assert.Contains("Calc フィールドは計算式の結果", result);
        Assert.Contains("</remarks>", result);
    }

    [Fact]
    public void BuildForField_WithOptions_AddsValuesSection() {
        var field = new KintoneFieldSchema {
            Label = "選択肢",
            FieldType = KintoneFieldType.CheckBox,
            Options = ["A", "B", "C"]
        };

        var result = _builder.BuildForField(field);

        Assert.Contains("<values>", result);
        Assert.Contains("/// - A", result);
        Assert.Contains("/// - B", result);
        Assert.Contains("/// - C", result);
        Assert.Contains("</values>", result);
    }

    [Fact]
    public void BuildForField_EscapesSpecialCharacters() {
        var field = new KintoneFieldSchema {
            Label = "A & B < C > D",
            FieldType = KintoneFieldType.SingleLineText
        };

        var result = _builder.BuildForField(field);

        Assert.Contains("A &amp; B &lt; C &gt; D", result);
    }

    // -----------------------------
    // BuildForSubTable
    // -----------------------------
    [Fact]
    public void BuildForSubTable_WithLabel_GeneratesSummary() {
        var sub = new KintoneSubTableSchema {
            Label = "明細"
        };

        var result = _builder.BuildForSubTable(sub);

        Assert.Contains("/// サブテーブル: 明細", result);
    }

    [Fact]
    public void BuildForSubTable_WithoutLabel_UsesDefault() {
        var sub = new KintoneSubTableSchema {
            Label = ""
        };

        var result = _builder.BuildForSubTable(sub);

        Assert.Contains("/// サブテーブル", result);
    }
}
