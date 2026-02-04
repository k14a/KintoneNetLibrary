using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
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
                    FieldCode = "customer_name",
                    FieldLabel = "顧客名",
                    FieldType = KintoneFieldType.SingleLineText
                },
                new() {
                    FieldCode = "order_items",
                    FieldLabel = "明細",
                    FieldType = KintoneFieldType.SubTable,
                    SubFields = new List<KintoneFieldMetadata> {
                        new() {
                            FieldCode = "item_name",
                            FieldLabel = "商品名",
                            FieldType = KintoneFieldType.SingleLineText
                        },
                        new() {
                            FieldCode = "quantity",
                            FieldLabel = "数量",
                            FieldType = KintoneFieldType.Number
                        },
                        new() {
                            FieldCode = "unit_price",
                            FieldLabel = "単価",
                            FieldType = KintoneFieldType.Number
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
