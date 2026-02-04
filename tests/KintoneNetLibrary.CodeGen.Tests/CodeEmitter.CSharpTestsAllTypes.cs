using KintoneNetLibrary.CodeGen.Application.Emitters;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class CSharpCodeEmitterAllTypesTests {
    private readonly INameConverter _converter = new CSharpNameConverter();
    private readonly ITypeMapper _mapper = new CSharpTypeMapper();
    private readonly INameConverterFactory _converterFactory;
    private readonly ITypeMapperFactory _mapperFactory;
    private readonly IXmlCommentBuilder _xmlCommentBuilder;
    private readonly ISubTableEmitter _subTableEmitter;
    private readonly IHelperClassEmitter _helperClassEmitter;
    private readonly ILogger<CSharpCodeEmitter>? _logger = null;

    [Fact]
    public void EmitAllFieldTypesMatchesSnapshot() {
        // var emitter = new CSharpCodeEmitter(this._converter, this._mapper);
        var emitter = new CSharpCodeEmitter(this._converterFactory, this._mapperFactory, this._xmlCommentBuilder, this._subTableEmitter, this._helperClassEmitter, this._logger);

        var metadata = new KintoneAppMetadata {
            AppId = 3,
            Fields = new List<KintoneFieldMetadata> {
                new() { FieldCode = "text", FieldLabel = "テキスト", FieldType = KintoneFieldType.SingleLineText },
                new() { FieldCode = "number", FieldLabel = "数量", FieldType = KintoneFieldType.Number },
                new() { FieldCode = "date", FieldLabel = "日付", FieldType = KintoneFieldType.Date },
                new() { FieldCode = "datetime", FieldLabel = "日時", FieldType = KintoneFieldType.DateTime },
                new() { FieldCode = "time", FieldLabel = "時間", FieldType = KintoneFieldType.Time },
                new() { FieldCode = "checkbox", FieldLabel = "チェック", FieldType = KintoneFieldType.CheckBox },
                new() { FieldCode = "multi", FieldLabel = "選択肢", FieldType = KintoneFieldType.MultiSelect },
                new() { FieldCode = "file", FieldLabel = "添付ファイル", FieldType = KintoneFieldType.File },
                new() { FieldCode = "user", FieldLabel = "担当者", FieldType = KintoneFieldType.UserSelect },
                new() { FieldCode = "group", FieldLabel = "グループ", FieldType = KintoneFieldType.GroupSelect },
                new() { FieldCode = "org", FieldLabel = "組織", FieldType = KintoneFieldType.OrganizationSelect },

                // サブテーブル
                new() {
                    FieldCode = "details",
                    FieldLabel = "明細",
                    FieldType = KintoneFieldType.SubTable,
                    SubFields = new List<KintoneFieldMetadata> {
                        new() { FieldCode = "item", FieldLabel = "商品名", FieldType = KintoneFieldType.SingleLineText },
                        new() { FieldCode = "qty", FieldLabel = "数量", FieldType = KintoneFieldType.Number },
                        new() { FieldCode = "price", FieldLabel = "単価", FieldType = KintoneFieldType.Number }
                    }
                }
            }
        };

        var options = new CSharpEmitterOptions {
            Namespace = "KintoneNetLibrary.Generated"
        };

        var code = emitter.Emit(metadata, options);

        Snapshot.Match(code);
    }
}
