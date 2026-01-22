using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class CSharpCodeEmitterTests {
    private readonly INameConverter _converter = new CSharpNameConverter();
    private readonly ITypeMapper _mapper = new CSharpTypeMapper();

    [Fact]
    public void EmitSimpleFieldsMatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(this._converter, this._mapper);

        var metadata = new KintoneAppMetadata {
            AppId = 1,
            Fields = [
                new() { FieldCode = "customer_name", FieldLabel = "顧客名", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "order_date", FieldLabel = "日付", FieldType = KintoneFieldType.Date }
            ]
        };

        var options = new CodeEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated"
        };

        var code = emitter.Emit(metadata, options);

        Snapshot.Match(code);
    }
}
