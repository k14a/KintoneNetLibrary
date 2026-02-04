using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class CSharpCodeEmitterRecordTests {
    private readonly INameConverter _converter = new CSharpNameConverter();
    private readonly ITypeMapper _mapper = new CSharpTypeMapper();

    [Fact]
    public void Emit_RecordEnabled_MatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(_converter, _mapper);

        var metadata = new KintoneAppMetadata {
            AppId = 5,
            Fields = new List<KintoneFieldMetadata> {
                new() { FieldCode = "text", FieldLabel = "テキスト", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "number", FieldLabel = "数量", FieldType = KintoneFieldType.Number }
            }
        };

        var options = new CodeEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated",
            UseRecord = true
        };

        var code = emitter.Emit(metadata, options);

        Snapshot.Match(code);
    }
}
