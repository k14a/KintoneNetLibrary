using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class CSharpCodeEmitterNullableTests {
    private readonly INameConverter _converter = new CSharpNameConverter();
    private readonly ITypeMapper _mapper = new CSharpTypeMapper();

    [Fact]
    public void Emit_NullableEnabled_MatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(this._converter, this._mapper);

        var metadata = new KintoneAppMetadata {
            AppId = 4,
            Fields = new List<KintoneFieldMetadata> {
                new() { FieldCode = "text", FieldLabel = "テキスト", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "number", FieldLabel = "数量", FieldType = KintoneFieldType.Number },
                new() { FieldCode = "date", FieldLabel = "日付", FieldType = KintoneFieldType.Date },
                new() { FieldCode = "file", FieldLabel = "添付", FieldType = KintoneFieldType.File }
            }
        };

        var options = new CodeEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated",
            NullableEnabled = true
        };

        var code = emitter.Emit(metadata, options);

        Snapshot.Match(code);
    }
}