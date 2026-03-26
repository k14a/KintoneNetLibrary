using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Domain.Interfaces;
using Snapshooter.Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.CodeEmitter.CSharp;

/// <summary>
/// C# コードエミッタのレコード型生成に関するテストクラス。
/// </summary>
public class CSharpCodeEmitterRecordTests {
    // --- Fake 実装群 ---
    /// <summary>
    /// C# 用の名前変換を提供するファクトリーのフェイク実装。
    /// </summary>
    private class FakeNameConverterFactory : INameConverterFactory {
        /// <summary>
        /// 指定された言語に対して C# 用の名前変換器を生成する。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された名前変換インスタンス</returns>
        public INameConverter Create(GenerateLanguages lang, NameTable? nameTable) => new CSharpNameConverter();
    }

    /// <summary>
    /// C# 用の型マッピングを提供するファクトリーのフェイク実装。
    /// </summary>
    private class FakeTypeMapperFactory : ITypeMapperFactory {
        /// <summary>
        /// 指定された言語に対して C# 用の型マッパーを生成する。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された型マッパーインスタンス</returns>
        public ITypeMapper Create(GenerateLanguages lang) => new CSharpTypeMapper();
    }

    /// <summary>
    /// XML コメントの生成を行うビルダーのフェイク実装。
    /// </summary>
    private class FakeXmlCommentBuilder : IXmlCommentBuilder {
        /// <summary>
        /// 指定されたフィールドのラベルを使用して XML コメントの summary タグを生成する。
        /// </summary>
        /// <param name="field">XML コメントを構築するフィールドスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForField(KintoneFieldSchema field) => $"    /// <summary>{field.Label}</summary>";

        /// <summary>
        /// 指定されたサブテーブルのラベルを使用して XML コメントの summary タグを生成する。
        /// </summary>
        /// <param name="sub">XML コメントを構築するサブテーブルスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForSubTable(KintoneSubTableSchema sub) => $"    /// <summary>{sub.Label}</summary>";
    }

    /// <summary>
    /// サブテーブルのコード生成を行うエミッタのフェイク実装。
    /// </summary>
    private class FakeSubTableEmitter : ISubTableEmitter {
        /// <summary>
        /// サブテーブルのコード生成を行うが、ここでは空文字列を返すフェイク実装。
        /// </summary>
        /// <param name="fieldCode">フィールドコード</param>
        /// <param name="schema">サブテーブルスキーマ</param>
        /// <param name="options">C# エミッタオプション</param>
        /// <returns>空文字列</returns>
        public string EmitSubTable(string fieldCode, KintoneSubTableSchema schema, CSharpEmitterOptions options) => string.Empty;

        public void SetNameConverter(INameConverter converter) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// サブテーブルのコード生成を行うが、ここでは空の GeneratedSubTableModel を返すフェイク実装。
        /// </summary>
        /// <param name="name">サブテーブルの名前</param>
        /// <param name="subTable">サブテーブルスキーマ</param>
        /// <param name="options">C# エミッタオプション</param>
        /// <returns>空の GeneratedSubTableModel インスタンス</returns>
        GeneratedSubTableModel ISubTableEmitter.EmitSubTable(string name, KintoneSubTableSchema subTable, CSharpEmitterOptions options) => new();
    }

    /// <summary>
    /// ヘルパークラスのコード生成を行うエミッタのフェイク実装。
    /// </summary>
    private class FakeHelperEmitter : IHelperClassEmitter {
        /// <summary>
        /// ヘルパークラスのコード生成を行うが、ここでは空の文字列列を返すフェイク実装。
        /// </summary>
        /// <param name="options">C# エミッタオプション</param>
        /// <returns>空の文字列列</returns>
        public IEnumerable<string> EmitHelperClasses(CSharpEmitterOptions options) => [];

        /// <summary>
        /// ヘルパークラスのコード生成を行うが、ここでは空の GeneratedHelperClass 列を返すフェイク実装。
        /// </summary>
        /// <param name="options">コードエミッタオプション</param>
        /// <returns>空の GeneratedHelperClass 列</returns>
        public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) => [];
    }

    /// <summary>
    /// 日時の提供を行うプロバイダーのフェイク実装。常に固定された日時を返す。
    /// </summary>
    private class FakeClock : IDateTimeProvider {
        /// <summary>
        /// 現在の日時を返すプロパティ。ここでは常に 2024年1月1日 を返すフェイク実装。
        /// </summary>
        public DateTime Now => new(2024, 1, 1);

        /// <summary>
        /// 現在の UTC 日時を返すプロパティ。ここでは常に 2024年1月1日 を返すフェイク実装。
        /// </summary>
        public DateTime UtcNow => new(2024, 1, 1);
    }

    /// <summary>
    /// レコード型を有効にしてコード生成を行い、生成されたコードがスナップショットと一致することを検証するテスト。
    /// </summary>
    [Fact]
    public void Emit_RecordEnabled_MatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(
            new FakeTypeMapperFactory(),
            new FakeXmlCommentBuilder(),
            new FakeSubTableEmitter(),
            new FakeHelperEmitter(),
            new FakeClock(),
            logger: null
        );

        var schema = new KintoneAppSchema {
            AppId = 5,
            AppName = "RecordTestApp",
            Revision = 1,
            Fields = [
                new() { FieldCode = "text", Label = "テキスト", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "number", Label = "数量", FieldType = KintoneFieldType.Number }
            ]
        };

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated",
            MainClassName = "RecordModel",
            UseRecord = true,
            UseKintoneNetLibrary = false
        };

        var result = emitter.Emit(schema, options);

        Snapshot.Match(result.MainModelCode);
    }
}
