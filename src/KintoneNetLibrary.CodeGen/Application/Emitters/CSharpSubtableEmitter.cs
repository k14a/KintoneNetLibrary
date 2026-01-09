using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

public class CSharpSubTableEmitter(
    INameConverter names,
    ITypeMapper types,
    IXmlCommentBuilder xml) : ISubTableEmitter {

    private readonly INameConverter _names = names;
    private readonly ITypeMapper _types = types;
    private readonly IXmlCommentBuilder _xml = xml;

    public GeneratedSubTableModel EmitSubTable(string name, KintoneSubTableSchema subTable, CodeEmitterOptions options) {
        var sb = new StringBuilder();

        this.EmitUsingSection(sb, options);

        // namespace
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();

        // class name
        var className = $"SubTable{name}";

        // XML コメント（サブテーブル用）
        sb.AppendLine(this._xml.BuildForSubTable(subTable));

        // class header
        this.EmitClassName(sb, className, options);
        // sb.AppendLine($"public partial class {className}");
        sb.AppendLine("{");

        // properties
        foreach (var field in subTable.Fields) {
            this.EmitProperty(sb, field, options);
        }

        sb.AppendLine("}");

        return new GeneratedSubTableModel {
            ClassName = className,
            Code = sb.ToString()
        };
    }

    private void EmitUsingSection(StringBuilder sb, CodeEmitterOptions options) {
        sb.AppendLine("using System;");
        if (options.UseKintoneNetLibrary) {
            sb.AppendLine("using KintoneNetLibrary.Domain.Entities;");
            sb.AppendLine("using KintoneNetLibrary.Domain.Access;");
            sb.AppendLine("using KintoneNetLibrary.Domain.Attributes;");
        }
        sb.AppendLine();
    }
    private void EmitClassName(StringBuilder sb, string className, CodeEmitterOptions options) {
        sb.Append($"public partial class {className}");
        if (options.UseKintoneNetLibrary) {
            sb.Append(" : KintoneSubTableBase");
        }
        sb.AppendLine();
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