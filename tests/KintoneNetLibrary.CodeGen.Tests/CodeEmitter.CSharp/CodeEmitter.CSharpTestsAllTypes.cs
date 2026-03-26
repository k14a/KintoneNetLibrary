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
/// 全フィールドタイプを含むスキーマを用いて、C#コードエミッタの出力が期待通りであることを確認するテスト。
/// </summary>
public class CSharpCodeEmitterAllTypesTests {
    // --- Fake 実装群（前のテストと同じ） ---
    /// <summary>
    /// C#コードエミッタ用の名前変換器を返すファクトリーのフェイク実装。
    /// </summary>
    private class FakeNameConverterFactory : INameConverterFactory {
        /// <summary>
        /// C#コードエミッタ用の名前変換器を返す。実際には、全ての言語で同じCSharpNameConverterを返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された名前変換インスタンス</returns>
        public INameConverter Create(GenerateLanguages lang, NameTable? nameTable) => new CSharpNameConverter();
    }

    /// <summary>
    /// C#コードエミッタ用の型マッパーを返すファクトリーのフェイク実装。
    /// </summary>
    private class FakeTypeMapperFactory : ITypeMapperFactory {
        /// <summary>
        /// C#コードエミッタ用の型マッパーを返す。実際には、全ての言語で同じCSharpTypeMapperを返す。
        /// </summary>
        /// <param name="lang">生成する言語</param>
        /// <returns>作成された型マッパーインスタンス</returns>
        public ITypeMapper Create(GenerateLanguages lang) => new CSharpTypeMapper();
    }

    /// <summary>
    /// XMLコメントのビルダーのフェイク実装。フィールドやサブテーブルのラベルをそのままXMLコメントとして返す。
    /// </summary>
    private class FakeXmlCommentBuilder : IXmlCommentBuilder {
        /// <summary>
        /// フィールドのラベルをXMLコメントとして返す。実際の実装では、特殊文字のエスケープや改行の処理などが必要になるが、テスト用のフェイク実装では単純にラベルを返すだけにする。
        /// </summary>
        /// <param name="field">XMLコメントを構築するフィールドスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForField(KintoneFieldSchema field) => $"    /// <summary>{field.Label}</summary>";

        /// <summary>
        /// サブテーブルのラベルをXMLコメントとして返す。実際の実装では、特殊文字のエスケープや改行の処理などが必要になるが、テスト用のフェイク実装では単純にラベルを返すだけにする。
        /// </summary>
        /// <param name="sub">XMLコメントを構築するサブテーブルスキーマ</param>
        /// <returns>構築されたXMLコメント文字列</returns>
        public string BuildForSubTable(KintoneSubTableSchema sub) => $"    /// <summary>{sub.Label}</summary>";
    }

    /// <summary>
    /// サブテーブルエミッタのフェイク実装。実際の実装では、サブテーブルのフィールドをコードに変換するロジックが必要になるが、テスト用のフェイク実装では単純に空文字列を返すだけにする。
    /// </summary>
    private class FakeSubTableEmitter : ISubTableEmitter {
        /// <summary>
        /// サブテーブルのフィールドコードをコードに変換して返す。実際の実装では、サブテーブルのフィールドをコードに変換するロジックが必要になるが、テスト用のフェイク実装では単純に空文字列を返すだけにする。
        /// </summary>
        /// <param name="fieldCode">変換するフィールドコード</param>
        /// <param name="schema">変換するサブテーブルスキーマ</param>
        /// <param name="options">コード生成オプション</param>
        /// <returns>変換されたコード文字列</returns>
        public string EmitSubTable(string fieldCode, KintoneSubTableSchema schema, CSharpEmitterOptions options) => string.Empty;

        public void SetNameConverter(INameConverter converter) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// サブテーブルのフィールドコードをコードに変換して返す。実際の実装では、サブテーブルのフィールドをコードに変換するロジックが必要になるが、テスト用のフェイク実装では単純に空文字列を返すだけにする。
        /// </summary>
        /// <param name="name">変換するフィールドコードの名前</param>
        /// <param name="subTable">変換するサブテーブルスキーマ</param>
        /// <param name="options">コード生成オプション</param>
        /// <returns>変換されたコード文字列</returns>
        GeneratedSubTableModel ISubTableEmitter.EmitSubTable(string name, KintoneSubTableSchema subTable, CSharpEmitterOptions options) => new();
    }

    /// <summary>
    /// ヘルパークラスエミッタのフェイク実装。実際の実装では、コード生成に必要なヘルパークラスをコードに変換するロジックが必要になるが、テスト用のフェイク実装では単純に空の列挙を返すだけにする。
    /// </summary>
    private class FakeHelperEmitter : IHelperClassEmitter {
        /// <summary>
        /// コード生成に必要なヘルパークラスをコードに変換して返す。実際の実装では、コード生成に必要なヘルパークラスをコードに変換するロジックが必要になるが、テスト用のフェイク実装では単純に空の列挙を返すだけにする。
        /// </summary>
        /// <param name="options">コード生成オプション</param>
        /// <returns>変換されたヘルパークラスの列挙</returns>
        public IEnumerable<string> EmitHelperClasses(CSharpEmitterOptions options) => [];

        /// <summary>
        /// コード生成に必要なヘルパークラスをコードに変換して返す。実際の実装では、コード生成に必要なヘルパークラスをコードに変換するロジックが必要になるが、テスト用のフェイク実装では単純に空の列挙を返すだけにする。
        /// </summary>
        /// <param name="options">コード生成オプション</param>
        /// <returns>変換されたヘルパークラスの列挙</returns>
        public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) => [];
    }

    /// <summary>
    /// 日時を固定して返すクロックのフェイク実装。実際の実装では、現在日時を返すロジックが必要になるが、テスト用のフェイク実装では単純に固定日時を返すだけにする。
    /// </summary>
    private class FakeClockEmitter : IDateTimeProvider {
        /// <summary>
        /// 現在日時を固定して返す。実際の実装では、現在日時を返すロジックが必要になるが、テスト用のフェイク実装では単純に固定日時を返すだけにする。
        /// </summary>
        public DateTime Now => new(2024, 1, 1);

        /// <summary>
        /// 現在日時を固定して返す。実際の実装では、現在日時を返すロジックが必要になるが、テスト用のフェイク実装では単純に固定日時を返すだけにする。
        /// </summary>
        public DateTime UtcNow => new(2024, 1, 1);
    }

    /// <summary>
    /// 全フィールドタイプを含むスキーマを用いて、C#コードエミッタの出力が期待通りであることを確認するテスト。スナップショットテストを使用して、生成されたコードが事前に保存されたスナップショットと一致することを検証する。
    /// </summary>
    [Fact]
    public void EmitAllFieldTypesMatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(
            new FakeTypeMapperFactory(),
            new FakeXmlCommentBuilder(),
            new FakeSubTableEmitter(),
            new FakeHelperEmitter(),
            new FakeClockEmitter(),
            logger: null
        );

        // --- metadata → schema に変換 ---
        var schema = new KintoneAppSchema {
            AppId = 3,
            AppName = "AllTypesApp",
            Revision = 1,
            Fields = [
                new() { FieldCode = "text", Label = "テキスト", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "number", Label = "数量", FieldType = KintoneFieldType.Number },
                new() { FieldCode = "date", Label = "日付", FieldType = KintoneFieldType.Date },
                new() { FieldCode = "datetime", Label = "日時", FieldType = KintoneFieldType.DateTime },
                new() { FieldCode = "time", Label = "時間", FieldType = KintoneFieldType.Time },
                new() { FieldCode = "checkbox", Label = "チェック", FieldType = KintoneFieldType.CheckBox },
                new() { FieldCode = "multi", Label = "選択肢", FieldType = KintoneFieldType.MultiSelect },
                new() { FieldCode = "file", Label = "添付ファイル", FieldType = KintoneFieldType.File },
                new() { FieldCode = "user", Label = "担当者", FieldType = KintoneFieldType.UserSelect },
                new() { FieldCode = "group", Label = "グループ", FieldType = KintoneFieldType.GroupSelect },
                new() { FieldCode = "org", Label = "組織", FieldType = KintoneFieldType.OrganizationSelect }
            ],
            SubTables = [
                new KintoneSubTableSchema {
                    FieldCode = "details",
                    Label = "明細",
                    Fields = [
                        new() { FieldCode = "item", Label = "商品名", FieldType = KintoneFieldType.SingleLineText },
                        new() { FieldCode = "qty", Label = "数量", FieldType = KintoneFieldType.Number },
                        new() { FieldCode = "price", Label = "単価", FieldType = KintoneFieldType.Number }
                    ]
                }
            ]
        };

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated",
            MainClassName = "AllTypesModel",
            UseKintoneNetLibrary = false
        };

        var result = emitter.Emit(schema, options);

        Snapshot.Match(result.MainModelCode);
    }
}
