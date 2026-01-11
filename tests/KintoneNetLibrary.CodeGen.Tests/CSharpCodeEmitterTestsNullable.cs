using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
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
                new() { Code = "text", Label = "テキスト", Type = KintoneFieldType.SingleLineText },
                new() { Code = "number", Label = "数量", Type = KintoneFieldType.Number },
                new() { Code = "date", Label = "日付", Type = KintoneFieldType.Date },
                new() { Code = "file", Label = "添付", Type = KintoneFieldType.File }
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