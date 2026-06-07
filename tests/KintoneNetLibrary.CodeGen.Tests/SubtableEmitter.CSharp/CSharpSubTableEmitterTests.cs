using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.SubTableEmitter.CSharp;

/// <summary>
/// CSharpSubTableEmitter の単体テスト。
/// </summary>
public class CSharpSubTableEmitterTests {
    // --- Fake 実装群 ---
    /// <summary>
    /// テスト用の INameConverterFactory 実装。常に CSharpNameConverter を返す。
    /// </summary>
    private class FakeNameConverterFactory : INameConverterFactory {
        /// <summary>
        /// 指定された言語に関係なく、常に CSharpNameConverter のインスタンスを返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>CSharpNameConverter のインスタンス</returns>
        public INameConverter Create(GenerateLanguages lang, NameTable? nameTable) => new CSharpNameConverter();
    }

    /// <summary>
    /// テスト用の ITypeMapperFactory 実装。常に CSharpTypeMapper を返す。
    /// </summary>
    private class FakeTypeMapperFactory : ITypeMapperFactory {
        /// <summary>
        /// 指定された言語に関係なく、常に CSharpTypeMapper のインスタンスを返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>CSharpTypeMapper のインスタンス</returns>
        public ITypeMapper Create(GenerateLanguages lang) => new CSharpTypeMapper();
    }

    /// <summary>
    /// テスト用の IXmlCommentBuilder 実装。フィールドとサブテーブルのラベルを XML コメントとして生成する。
    /// </summary>
    private class FakeXmlCommentBuilder : IXmlCommentBuilder {
        /// <summary>
        /// フィールドのラベルを XML コメントとして生成する。例: "    /// <summary>商品名</summary>"
        /// </summary>
        /// <param name="field">フィールドスキーマ</param>
        /// <returns>XML コメント文字列</returns>
        public string BuildForField(KintoneFieldSchema field) => $"    /// <summary>{field.Label}</summary>";

        /// <summary>
        /// サブテーブルのラベルを XML コメントとして生成する。例: "/// <summary>明細</summary>"
        /// </summary>
        /// <param name="sub">サブテーブルスキーマ</param>
        /// <returns>XML コメント文字列</returns>
        public string BuildForSubTable(KintoneSubTableSchema sub) => $"/// <summary>{sub.Label}</summary>";
    }

    /// <summary>
    /// テスト用の ILogger<CSharpSubTableEmitter> 実装。ログは出力せず、スコープも使用しない。
    /// </summary>
    private class FakeLogger : ILogger<CSharpSubTableEmitter> {
        /// <summary>
        /// スコープを開始するが、実際には何もしない。常に NullScope を返す。
        /// </summary>
        /// <typeparam name="TState">スコープの状態の型</typeparam>
        /// <param name="state">スコープの状態</param>
        /// <returns>常に NullScope のインスタンス</returns>
        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        /// <summary>
        /// すべてのログレベルでログ出力を無効にする。常に false を返す。
        /// </summary>
        /// <param name="logLevel">ログレベル</param>
        /// <returns>常に false</returns>
        public bool IsEnabled(LogLevel logLevel) => false;

        /// <summary>
        /// ログ出力を行わない。呼び出されても何もしない。
        /// </summary>
        /// <typeparam name="TState">ログの状態の型</typeparam>
        /// <param name="logLevel">ログレベル</param>
        /// <param name="eventId">イベントId</param>
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
        /// IDisposable の実装で、スコープを開始しても何もしない。常に同じ NullScope インスタンスを返す。
        /// </summary>
        private class NullScope : IDisposable {
            /// <summary>
            /// NullScope のシングルトンインスタンス。スコープを開始しても何もしないため、常に同じインスタンスを返す。
            /// </summary>
            public static readonly NullScope Instance = new();

            /// <summary>
            /// IDisposable の実装で、スコープを終了しても何もしない。NullScope はスコープの開始と終了を管理しないため、このメソッドは空実装となる。
            /// </summary>
            public void Dispose() { }
        }
    }

    /// <summary>
    /// EmitSubTable メソッドが、指定されたサブテーブルスキーマに基づいて期待される C# コードを生成することを検証するテスト。
    /// </summary>
    [Fact]
    public void EmitSubTable_GeneratesExpectedCode() {
        // Arrange
        var emitter = new CSharpSubTableEmitter(
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
        var result = emitter.EmitSubTable("order_items", subTable, options, new CSharpNameConverter());

        // Assert: クラス名が正しい
        Assert.Equal("SubTableOrderItems", result.ClassName);

        // Assert: スナップショットでコード全体を検証
        Snapshot.Match(result.Code);
    }

    /// <summary>
    /// EmitSubTable メソッドが、同じフィールドコードのサブテーブルに対して一意のクラス名を生成することを検証するテスト。
    /// </summary>
    [Fact]
    public void EmitSubTable_GeneratesUniqueClassNames_WhenDuplicated() {
        // Arrange
        var emitter = new CSharpSubTableEmitter(
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
        var converter = new CSharpNameConverter();
        var first = emitter.EmitSubTable("details", subTable, options, converter);
        var second = emitter.EmitSubTable("details", subTable, options, converter);

        // Assert
        Assert.Equal("SubTableDetails", first.ClassName);
        Assert.Equal("SubTableDetails1", second.ClassName);
    }
}
