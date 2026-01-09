using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// C# コードエミッター
/// </summary>
/// <param name="names"></param>
/// <param name="types"></param>
/// <param name="xml"></param>
/// <param name="subTableEmitter"></param>
/// <param name="helperEmitter"></param>
public class CSharpCodeEmitter(
    INameConverter names,
    ITypeMapper types,
    IXmlCommentBuilder xml,
    ISubTableEmitter subTableEmitter,
    IHelperClassEmitter helperEmitter) : ICodeEmitter {

    private readonly INameConverter _names = names;
    private readonly ITypeMapper _types = types;
    private readonly IXmlCommentBuilder _xml = xml;
    private readonly ISubTableEmitter _subTableEmitter = subTableEmitter;
    private readonly IHelperClassEmitter _helperEmitter = helperEmitter;

    private static readonly HashSet<string> SystemFieldCodes = new(StringComparer.OrdinalIgnoreCase) {
        "レコード番号",
        "record_id",
        "作成者",
        "creator",
        "作成日時",
        "createdTime",
        "更新者",
        "modifier",
        "更新日時",
        "updatedTime",
        "ステータス",
        "status",
        "カテゴリー",
        "category",
        "作業者",
        "assignee"
    };

    private static bool IsSystemField(KintoneFieldSchema field) {
        return SystemFieldCodes.Contains(field.FieldCode);
    }


    public GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options) {
        var result = new GeneratedModelResult {
            // 1. メインモデル生成
            MainModelCode = this.EmitMainModel(schema, options),
            Revision = schema.Revision
        };

        // 2. サブテーブル生成
        foreach (var sub in schema.SubTables) {
            result.SubTableModels.Add(
                this._subTableEmitter.EmitSubTable(sub.FieldCode, sub, options)
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

        // using セクション
        this.EmitUsingSection(sb, options);

        // namespace
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();

        // class header
        this.EmitClassName(sb, schema, options);
        sb.AppendLine("{");

        // properties
        foreach (var field in schema.Fields) {
            // システムフィールドはスキップ
            if (options.UseKintoneNetLibrary && IsSystemField(field)) { continue; }
            this.EmitProperty(sb, field, options);
        }

        foreach (var sub in schema.SubTables) {
            this.EmitSubTables(sb, sub, options);
        }

        sb.AppendLine("}");
        return sb.ToString();
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
    private void EmitClassName(StringBuilder sb, KintoneAppSchema schema, CodeEmitterOptions options) {
        var className = options.MainClassName;
        sb.Append($"public partial class {className}");
        if (options.UseKintoneNetLibrary) {
            sb.Append(" : KintoneModelBase<" + className + ">");
        }
        sb.AppendLine();
    }
    private void EmitProperty(StringBuilder sb, KintoneFieldSchema field, CodeEmitterOptions options) {
        var propName = this._names.ToPropertyName(field.Label, field.FieldCode);
        var typeName = this._types.MapType(field, options.UseKintoneNetLibrary);

        // XML コメント
        var xml = this._xml.BuildForField(field);
        sb.AppendLine(xml);

        // KintoneItemAttribute
        this.EmitAttributes(sb, field, options);

        // プロパティ本体
        sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
        sb.AppendLine();
    }
    private void EmitSubTables(StringBuilder sb, KintoneSubTableSchema schema, CodeEmitterOptions options) {
        var propName = this._names.ToPropertyName(schema.Label, schema.FieldCode);
        var classNameSub = this._names.ToClassName(schema.Label, schema.FieldCode);

        // XML コメント
        sb.AppendLine(this._xml.BuildForSubTable(schema));

        // KintoneItemAttribute
        if (options.UseKintoneNetLibrary) {
            sb.AppendLine($"    [KintoneItem(FieldCode = \"{schema.FieldCode}\", FieldType = KintoneFieldType.SubTable)]");
        }

        // プロパティ本体
        sb.AppendLine($"    public List<SubTable{classNameSub}> {propName} {{ get; set; }}");
        sb.AppendLine();
    }
    private void EmitAttributes(StringBuilder sb, KintoneFieldSchema field, CodeEmitterOptions options) {
        if (options.UseKintoneNetLibrary) {
            sb.AppendLine($"    [KintoneItem(FieldCode = \"{field.FieldCode}\")]");
        }
    }
}
