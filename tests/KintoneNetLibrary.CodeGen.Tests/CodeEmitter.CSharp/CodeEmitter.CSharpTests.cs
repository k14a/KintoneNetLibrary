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
/// C#コードエミッタのテストクラス
/// </summary>
public class CSharpCodeEmitterTests {
    /// <summary>
    /// テスト用のフェイク実装クラス
    /// </summary>
    private class FakeNameConverterFactory : INameConverterFactory {
        /// <summary>
        /// 名前変換の作成
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された名前変換インスタンス</returns>
        public INameConverter Create(GenerateLanguages lang, NameTable? nameTable) => new CSharpNameConverter();
    }

    /// <summary>
    /// テスト用のフェイク実装クラス
    /// </summary>
    private class FakeTypeMapperFactory : ITypeMapperFactory {
        /// <summary>
        /// 型マッパーの作成
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された型マッパーインスタンス</returns>
        public ITypeMapper Create(GenerateLanguages lang) => new CSharpTypeMapper();
    }

    /// <summary>
    /// テスト用のフェイク実装クラス
    /// </summary>
    private class FakeXmlCommentBuilder : IXmlCommentBuilder {
        /// <summary>
        /// フィールドのXMLコメントを構築します。
        /// </summary>
        /// <param name="field">XMLコメントを構築するフィールドスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForField(KintoneFieldSchema field)
            => $"    /// <summary>{field.Label}</summary>";

        /// <summary>
        /// サブテーブルのXMLコメントを構築します。
        /// </summary>
        /// <param name="sub">XMLコメントを構築するサブテーブルスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForSubTable(KintoneSubTableSchema sub)
            => $"    /// <summary>{sub.Label}</summary>";
    }

    /// <summary>
    /// テスト用のフェイク実装クラス
    /// </summary>
    private class FakeSubTableEmitter : ISubTableEmitter {
        /// <summary>
        /// サブテーブルのコードを生成します。
        /// </summary>
        /// <param name="fieldCode">フィールドコード</param>
        /// <param name="schema">サブテーブルスキーマ</param>
        /// <param name="options">エミッタオプション</param>
        /// <returns>生成されたサブテーブルコード</returns>
        public string EmitSubTable(string fieldCode, KintoneSubTableSchema schema, CSharpEmitterOptions options) => string.Empty;

        /// <summary>
        /// サブテーブルのモデルを生成します。
        /// </summary>
        /// <param name="name">モデル名</param>
        /// <param name="subTable">サブテーブルスキーマ</param>
        /// <param name="options">エミッタオプション</param>
        /// <param name="nameConverter">名前変換</param>
        /// <returns>生成されたサブテーブルモデル</returns>
        GeneratedSubTableModel ISubTableEmitter.EmitSubTable(string name, KintoneSubTableSchema subTable, CSharpEmitterOptions options, INameConverter nameConverter) => new();
    }

    /// <summary>
    /// テスト用のフェイク実装クラス
    /// </summary>
    private class FakeHelperEmitter : IHelperClassEmitter {
        /// <summary>
        /// ヘルパークラスのコードを生成します。
        /// </summary>
        /// <param name="options">エミッタオプション</param>
        /// <returns>生成されたヘルパークラスコードの列挙</returns>
        public IEnumerable<string> EmitHelperClasses(CSharpEmitterOptions options) => [];

        /// <summary>
        /// ヘルパークラスのモデルを生成します。
        /// </summary>
        /// <param name="options">エミッタオプション</param>
        /// <returns>生成されたヘルパークラスモデルの列挙</returns>
        public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) => [];
    }

    /// <summary>
    /// テスト用のフェイク実装クラス
    /// </summary>
    private class FakeClock : IDateTimeProvider {
        /// <summary>
        /// 現在の日時を取得します。
        /// </summary>
        public DateTime Now => new(2024, 1, 1);

        /// <summary>
        /// 現在のUTC日時を取得します。
        /// </summary>
        public DateTime UtcNow => new(2024, 1, 1);
    }

    /// <summary>
    /// 単純なフィールドを持つKintoneアプリのスキーマをエミットし、結果がスナップショットと一致することを確認するテスト
    /// </summary>
    [Fact]
    public void EmitSimpleFieldsMatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(
            new FakeTypeMapperFactory(),
            new FakeXmlCommentBuilder(),
            new FakeSubTableEmitter(),
            new FakeHelperEmitter(),
            new FakeClock(),
            logger: null
        );

        var schema = new KintoneAppSchema {
            AppId = 1,
            AppName = "TestApp",
            Revision = 1,
            Fields = [
                new() { FieldCode = "customer_name", Label = "顧客名", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "order_date", Label = "日付", FieldType = KintoneFieldType.Date }
            ]
        };

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated",
            MainClassName = "TestAppModel",
            UseKintoneNetLibrary = false
        };

        var result = emitter.Emit(schema, options, new CSharpNameConverter());

        Snapshot.Match(result.MainModelCode);
    }
}
