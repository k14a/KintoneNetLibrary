using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class CSharpCodeEmitterAllTypesTests {
    private readonly INameConverter _converter = new CSharpNameConverter();
    private readonly ITypeMapper _mapper = new CSharpTypeMapper();

    [Fact]
    public void EmitAllFieldTypesMatchesSnapshot() {
        var emitter = new CSharpCodeEmitter(this._converter, this._mapper);

        var metadata = new KintoneAppMetadata {
            AppId = 3,
            Fields = new List<KintoneFieldMetadata> {
                new() { Code = "text", Label = "テキスト", Type = KintoneFieldType.SingleLineText },
                new() { Code = "number", Label = "数量", Type = KintoneFieldType.Number },
                new() { Code = "date", Label = "日付", Type = KintoneFieldType.Date },
                new() { Code = "datetime", Label = "日時", Type = KintoneFieldType.DateTime },
                new() { Code = "time", Label = "時間", Type = KintoneFieldType.Time },
                new() { Code = "checkbox", Label = "チェック", Type = KintoneFieldType.CheckBox },
                new() { Code = "multi", Label = "選択肢", Type = KintoneFieldType.MultiSelect },
                new() { Code = "file", Label = "添付ファイル", Type = KintoneFieldType.File },
                new() { Code = "user", Label = "担当者", Type = KintoneFieldType.UserSelect },
                new() { Code = "group", Label = "グループ", Type = KintoneFieldType.GroupSelect },
                new() { Code = "org", Label = "組織", Type = KintoneFieldType.OrganizationSelect },

                // サブテーブル
                new() {
                    Code = "details",
                    Label = "明細",
                    Type = KintoneFieldType.SubTable,
                    SubFields = new List<KintoneFieldMetadata> {
                        new() { Code = "item", Label = "商品名", Type = KintoneFieldType.SingleLineText },
                        new() { Code = "qty", Label = "数量", Type = KintoneFieldType.Number },
                        new() { Code = "price", Label = "単価", Type = KintoneFieldType.Number }
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
