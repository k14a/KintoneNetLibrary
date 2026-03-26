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
/// C# コードエミッタの Nullable 有効化に関するテストクラス。
/// </summary>
public class CSharpCodeEmitterNullableTests {
    // --- Fake 実装群 ---
    /// <summary>
    /// INameConverterFactory のテスト用のフェイク実装。
    /// </summary>
    private class FakeNameConverterFactory : INameConverterFactory {
        /// <summary>
        /// 指定された言語に対して、C# 用の名前変換器を返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された名前変換インスタンス</returns>
        public INameConverter Create(GenerateLanguages lang, NameTable? nameTable) => new CSharpNameConverter();
    }

    /// <summary>
    /// ITypeMapperFactory のテスト用のフェイク実装。
    /// </summary>
    private class FakeTypeMapperFactory : ITypeMapperFactory {
        /// <summary>
        /// 指定された言語に対して、C# 用の型マッパーを返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された型マッパーインスタンス</returns>
        public ITypeMapper Create(GenerateLanguages lang) => new CSharpTypeMapper();
    }

    /// <summary>
    /// IXmlCommentBuilder のテスト用のフェイク実装。
    /// </summary>
    private class FakeXmlCommentBuilder : IXmlCommentBuilder {
        /// <summary>
        /// 指定されたフィールドのラベルを使用して、XML コメントの summary タグを構築する。
        /// </summary>
        /// <param name="field">XML コメントを構築するフィールドスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForField(KintoneFieldSchema field) => $"    /// <summary>{field.Label}</summary>";

        /// <summary>
        /// 指定されたサブテーブルのラベルを使用して、XML コメントの summary タグを構築する。
        /// </summary>
        /// <param name="sub">XML コメントを構築するサブテーブルスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForSubTable(KintoneSubTableSchema sub) => $"    /// <summary>{sub.Label}</summary>";
    }

    /// <summary>
    /// ISubTableEmitter のテスト用のフェイク実装。
    /// </summary>
    private class FakeSubTableEmitter : ISubTableEmitter {
        /// <summary>
        /// サブテーブルのフィールドコード、スキーマ、およびエミッタオプションを受け取り、サブテーブルのコードを生成する。
        /// </summary>
        /// <param name="fieldCode">サブテーブルのフィールドコード</param>
        /// <param name="schema">サブテーブルのスキーマ</param>
        /// <param name="options">エミッタオプション</param>
        /// <returns>生成されたサブテーブルのコード</returns>
        public string EmitSubTable(string fieldCode, KintoneSubTableSchema schema, CSharpEmitterOptions options) => string.Empty;

        public void SetNameConverter(INameConverter converter) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// サブテーブルのコードを生成するためのモデルを構築する。
        /// </summary>
        /// <param name="name">サブテーブルの名前</param>
        /// <param name="subTable">サブテーブルのスキーマ</param>
        /// <param name="options">エミッタオプション</param>
        /// <returns>生成されたサブテーブルモデル</returns>
        GeneratedSubTableModel ISubTableEmitter.EmitSubTable(string name, KintoneSubTableSchema subTable, CSharpEmitterOptions options) => new();
    }

    /// <summary>
    /// IHelperClassEmitter のテスト用のフェイク実装。
    /// </summary>
    private class FakeHelperEmitter : IHelperClassEmitter {
        /// <summary>
        /// エミッタオプションを受け取り、必要なヘルパークラスのコードを生成する。
        /// </summary>
        /// <param name="options">エミッタオプション</param>
        /// <returns>生成されたヘルパークラスのコード</returns>
        public IEnumerable<string> EmitHelperClasses(CSharpEmitterOptions options) => [];

        /// <summary>
        /// エミッタオプションを受け取り、必要なヘルパークラスのモデルを生成する。
        /// </summary>
        /// <param name="options">エミッタオプション</param>
        /// <returns>生成されたヘルパークラスのモデル</returns>
        public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) => [];
    }

    /// <summary>
    /// IDateTimeProvider のテスト用のフェイク実装。常に固定された日時を返す。
    /// </summary>
    private class FakeClock : IDateTimeProvider {
        /// <summary>
        /// 常に 2024-01-01 の日時を返す Now プロパティ。
        /// </summary>
        public DateTime Now => new(2024, 1, 1);

        /// <summary>
        /// 常に 2024-01-01 の日時を返す UtcNow プロパティ。
        /// </summary>
        public DateTime UtcNow => new(2024, 1, 1);
    }

    /// <summary>
    /// NullableEnabled オプションが有効な場合のコード生成結果がスナップショットと一致することを検証するテスト。
    /// </summary>
    [Fact]
    public void Emit_NullableEnabled_MatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(
            new FakeTypeMapperFactory(),
            new FakeXmlCommentBuilder(),
            new FakeSubTableEmitter(),
            new FakeHelperEmitter(),
            new FakeClock(),
            logger: null
        );

        var schema = new KintoneAppSchema {
            AppId = 4,
            AppName = "NullableTestApp",
            Revision = 1,
            Fields = [
                new() { FieldCode = "text", Label = "テキスト", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "number", Label = "数量", FieldType = KintoneFieldType.Number },
                new() { FieldCode = "date", Label = "日付", FieldType = KintoneFieldType.Date },
                new() { FieldCode = "file", Label = "添付", FieldType = KintoneFieldType.File }
            ]
        };

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated",
            MainClassName = "NullableModel",
            NullableEnabled = true,
            UseKintoneNetLibrary = false
        };

        var result = emitter.Emit(schema, options);

        Snapshot.Match(result.MainModelCode);
    }
}
