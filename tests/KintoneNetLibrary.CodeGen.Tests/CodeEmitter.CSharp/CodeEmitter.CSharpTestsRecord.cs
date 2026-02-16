using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Infrastructure.Interfaces;
using Snapshooter.Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.CodeEmitter.CSharp;

public class CSharpCodeEmitterRecordTests {
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

    private class FakeClock : IDateTimeProvider {
        public DateTime Now => new(2024, 1, 1);

        public DateTime UtcNow => new(2024, 1, 1);
    }

    [Fact]
    public void Emit_RecordEnabled_MatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(
            new FakeNameConverterFactory(),
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
