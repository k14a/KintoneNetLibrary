using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

public class CSharpSubtableEmitter(
    INameConverter names,
    ITypeMapper types,
    IXmlCommentBuilder xml) : ISubtableEmitter {

    private readonly INameConverter _names = names;
    private readonly ITypeMapper _types = types;
    private readonly IXmlCommentBuilder _xml = xml;

    public GeneratedSubtableModel EmitSubtable(KintoneSubtableSchema subtable, CodeEmitterOptions options) {
        var sb = new StringBuilder();

        // namespace
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();

        // class name
        var className = this._names.ToClassName(subtable.Label, subtable.FieldCode);

        // XML コメント（サブテーブル用）
        sb.AppendLine(this._xml.BuildForSubtable(subtable));

        // class header
        sb.AppendLine($"public partial class {className}");
        sb.AppendLine("{");

        // properties
        foreach (var field in subtable.Fields)
        {
            this.EmitProperty(sb, field, options);
        }

        sb.AppendLine("}");

        return new GeneratedSubtableModel {
            ClassName = className,
            Code = sb.ToString()
        };
    }

    private void EmitProperty(StringBuilder sb, KintoneFieldSchema field, CodeEmitterOptions options) {
        var propName = this._names.ToPropertyName(field.Label, field.FieldCode);
        var typeName = this._types.MapType(field, options.UseKintoneNetLibrary);

        // XML コメント
        sb.AppendLine(this._xml.BuildForField(field));

        sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
        sb.AppendLine();
    }
}