using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class CSharpCodeEmitterSubtableTests {
    private readonly INameConverter _converter = new CSharpNameConverter();
    private readonly ITypeMapper _mapper = new CSharpTypeMapper();

    [Fact]
    public void EmitWithSubtableMatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(_converter, _mapper);

        var metadata = new KintoneAppMetadata {
            AppId = 2,
            Fields = new List<KintoneFieldMetadata> {
                new() {
                    Code = "customer_name",
                    Label = "顧客名",
                    Type = KintoneFieldType.SingleLineText
                },
                new() {
                    Code = "order_items",
                    Label = "明細",
                    Type = KintoneFieldType.SubTable,
                    SubFields = new List<KintoneFieldMetadata> {
                        new() {
                            Code = "item_name",
                            Label = "商品名",
                            Type = KintoneFieldType.SingleLineText
                        },
                        new() {
                            Code = "quantity",
                            Label = "数量",
                            Type = KintoneFieldType.Number
                        },
                        new() {
                            Code = "unit_price",
                            Label = "単価",
                            Type = KintoneFieldType.Number
                        }
                    }
                }
            }
        };

        var options = new CodeEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated"
        };

        var code = emitter.Emit(metadata, options);

        Snapshot.Match(code);
    }
}
