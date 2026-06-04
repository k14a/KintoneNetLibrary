using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.XmlCommentBuilder.CSharp;

/// <summary>
/// CSharpXmlCommentBuilder の単体テスト。
/// </summary>
public class CSharpXmlCommentBuilderTests {
    /// <summary>
    /// ILogger<CSharpXmlCommentBuilder> のダミー実装。テストではログ出力を行わないため、すべてのメソッドは無操作で実装されている。
    /// </summary>
    private class FakeLogger : ILogger<CSharpXmlCommentBuilder> {
        /// <summary>
        /// BeginScope メソッドは、ロガーのスコープを開始するためのものですが、テストではスコープ管理を行わないため、NullScope を返す実装となっている。
        /// </summary>
        /// <typeparam name="TState">スコープの状態の型</typeparam>
        /// <param name="state">スコープの状態</param>
        /// <returns>NullScope のインスタンス</returns>
        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        /// <summary>
        /// IsEnabled メソッドは、指定されたログレベルが有効かどうかを判断するためのものですが、テストではすべてのログレベルを無効とする実装となっている。
        /// </summary>
        /// <param name="logLevel">ログレベル</param>
        /// <returns>常に false を返す</returns>
        public bool IsEnabled(LogLevel logLevel) => false;

        /// <summary>
        /// Log メソッドは、ログエントリを記録するためのものですが、テストではログ出力を行わないため、空の実装となっている。
        /// </summary>
        /// <typeparam name="TState">ログエントリの状態の型</typeparam>
        /// <param name="logLevel">ログレベル</param>
        /// <param name="eventId">イベント ID</param>
        /// <param name="state">ログエントリの状態</param>
        /// <param name="exception">例外情報</param>
        /// <param name="formatter">ログエントリのフォーマッタ</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) { }

        /// <summary>
        /// NullScope クラスは、ILogger のスコープ管理のためのダミー実装であり、IDisposable を実装しているが、Dispose メソッドは何も行わない。
        /// </summary>
        private class NullScope : IDisposable {
            /// <summary>
            /// NullScope のインスタンスは、スコープ管理を行わないためのものであり、常に同じインスタンスを返すシングルトンパターンで実装されている。
            /// </summary>
            public static readonly NullScope Instance = new();

            /// <summary>
            /// Dispose メソッドは、スコープの終了を示すためのものですが、NullScope ではスコープ管理を行わないため、何も実装されていない。
            /// </summary>
            public void Dispose() { }
        }
    }

    /// <summary>
    /// CSharpXmlCommentBuilder のインスタンス。テストでは、FakeLogger を使用して ILogger<CSharpXmlCommentBuilder> を提供している。
    /// </summary>
    private readonly CSharpXmlCommentBuilder _builder = new(new FakeLogger());

    // -----------------------------
    // BuildForField
    // -----------------------------
    /// <summary>
    /// BuildForField メソッドが、Label プロパティを持つ KintoneFieldSchema に対して、正しい XML コメントの summary セクションを生成することを検証するテスト。
    /// </summary>
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

    /// <summary>
    /// BuildForField メソッドが、Label プロパティが空の KintoneFieldSchema に対して、デフォルトの summary セクションを生成することを検証するテスト。
    /// </summary>
    [Fact]
    public void BuildForField_WithoutLabel_UsesDefaultSummary() {
        var field = new KintoneFieldSchema {
            Label = "",
            FieldType = KintoneFieldType.SingleLineText
        };

        var result = _builder.BuildForField(field);

        Assert.Contains("/// Kintone field", result);
    }

    /// <summary>
    /// BuildForField メソッドが、FieldType が Calc の KintoneFieldSchema に対して、remarks セクションを追加することを検証するテスト。
    /// </summary>
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

    /// <summary>
    /// BuildForField メソッドが、Options プロパティを持つ KintoneFieldSchema に対して、values セクションを追加することを検証するテスト。
    /// </summary>
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

    /// <summary>
    /// BuildForField メソッドが、Label プロパティに特殊文字を含む KintoneFieldSchema に対して、XML エスケープされた summary セクションを生成することを検証するテスト。
    /// </summary>
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
    /// <summary>
    /// BuildForSubTable メソッドが、Label プロパティを持つ KintoneSubTableSchema に対して、正しい XML コメントの summary セクションを生成することを検証するテスト。
    /// </summary>
    [Fact]
    public void BuildForSubTable_WithLabel_GeneratesSummary() {
        var sub = new KintoneSubTableSchema {
            Label = "明細"
        };

        var result = _builder.BuildForSubTable(sub);

        Assert.Contains("/// サブテーブル: 明細", result);
    }

    /// <summary>
    /// BuildForSubTable メソッドが、Label プロパティが空の KintoneSubTableSchema に対して、デフォルトの summary セクションを生成することを検証するテスト。
    /// </summary>
    [Fact]
    public void BuildForSubTable_WithoutLabel_UsesDefault() {
        var sub = new KintoneSubTableSchema {
            Label = ""
        };

        var result = _builder.BuildForSubTable(sub);

        Assert.Contains("/// サブテーブル", result);
    }
}
