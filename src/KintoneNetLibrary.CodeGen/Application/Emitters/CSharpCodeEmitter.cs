using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

public class CSharpCodeEmitter(
    INameConverter names,
    ITypeMapper types,
    IXmlCommentBuilder xml,
    ISubtableEmitter subtableEmitter,
    IHelperClassEmitter helperEmitter) : ICodeEmitter {

    private readonly INameConverter _names = names;
    private readonly ITypeMapper _types = types;
    private readonly IXmlCommentBuilder _xml = xml;
    private readonly ISubtableEmitter _subtableEmitter = subtableEmitter;
    private readonly IHelperClassEmitter _helperEmitter = helperEmitter;

    public GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options) {
        var result = new GeneratedModelResult {
            // 1. メインモデル生成
            MainModelCode = this.EmitMainModel(schema, options)
        };

        // 2. サブテーブル生成
        foreach (var sub in schema.Subtables) {
            result.SubtableModels.Add(
                this._subtableEmitter.EmitSubtable(sub, options)
            );
        }

        // 3. pure モードなら補助クラス生成
        if (!options.UseKintoneNetLibrary) {
            result.HelperClasses.AddRange(
                this._helperEmitter.EmitHelperClasses(options)
            );
        }

        return result;
    }
    private string EmitMainModel(KintoneAppSchema schema, CodeEmitterOptions options) {
        var sb = new StringBuilder();

        // namespace
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();

        // class header
        var className = options.MainClassName;
        sb.AppendLine($"public partial class {className}");
        sb.AppendLine("{");

        // properties
        foreach (var field in schema.Fields) {
            this.EmitProperty(sb, field, options);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
    private void EmitProperty(StringBuilder sb, KintoneFieldSchema field, CodeEmitterOptions options) {
        var propName = this._names.ToPropertyName(field.Label, field.FieldCode);
        var typeName = this._types.MapType(field, options.UseKintoneNetLibrary);

        // XML コメント
        var xml = this._xml.BuildForField(field);
        sb.AppendLine(xml);

        sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
        sb.AppendLine();
    }
}
