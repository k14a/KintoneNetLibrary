using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
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
                new() { Code = "text", Label = "テキスト", Type = KintoneFieldType.SingleLineText },
                new() { Code = "number", Label = "数量", Type = KintoneFieldType.Number }
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
