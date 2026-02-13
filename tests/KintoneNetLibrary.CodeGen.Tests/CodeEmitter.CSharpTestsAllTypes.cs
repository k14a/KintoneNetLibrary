using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using Snapshooter.Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class CSharpCodeEmitterAllTypesTests {
    // --- Fake 実装群（前のテストと同じ） ---
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
            => $"    /// <summary>{sub.Label}</summary>";
    }

    private class FakeSubTableEmitter : ISubTableEmitter {
        public string EmitSubTable(string fieldCode, KintoneSubTableSchema schema, CSharpEmitterOptions options) => string.Empty;

        GeneratedSubTableModel ISubTableEmitter.EmitSubTable(string name, KintoneSubTableSchema subTable, CSharpEmitterOptions options) => new();
    }

    private class FakeHelperEmitter : IHelperClassEmitter {
        public IEnumerable<string> EmitHelperClasses(CSharpEmitterOptions options) => [];

        public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) => [];
    }

    [Fact]
    public void EmitAllFieldTypesMatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(
            new FakeNameConverterFactory(),
            new FakeTypeMapperFactory(),
            new FakeXmlCommentBuilder(),
            new FakeSubTableEmitter(),
            new FakeHelperEmitter(),
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
