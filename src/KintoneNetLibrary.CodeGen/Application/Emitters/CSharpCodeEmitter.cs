using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// C# コードエミッタ
/// </summary>
public class CSharpCodeEmitter(INameConverter nameConverter, ITypeMapper typeMapper) : ICodeEmitter {
    private readonly INameConverter _nameConverter = nameConverter;
    private readonly ITypeMapper _typeMapper = typeMapper;

    /// <summary>
    /// コードを生成する
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public string Emit(KintoneAppMetadata metadata, CodeEmitterOptions options) {
        var sb = new StringBuilder();

        // nullable
        if (options.NullableEnabled) {
            sb.AppendLine("#nullable enable");
        }

        // namespace
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();

        // メインクラス名（アプリ名は fields.json から取れないので AppId ベース）
        var mainClassName = $"App{metadata.AppId}";

        this.EmitMainClass(sb, metadata, mainClassName, options);

        // サブテーブルクラス
        foreach (var field in metadata.Fields.Where(f => f.Type == KintoneFieldType.SubTable)) {
            this.EmitSubtableClass(sb, field, options);
        }

        return sb.ToString();
    }

    /// <summary>
    /// メインクラスを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="metadata"></param>
    /// <param name="className"></param>
    /// <param name="options"></param>
    private void EmitMainClass(StringBuilder sb, KintoneAppMetadata metadata, string className, CodeEmitterOptions options) {
        sb.AppendLine($"public {(options.UseRecord ? "record" : "class")} {className}");
        sb.AppendLine("{");

        foreach (var field in metadata.Fields) {
            this.EmitProperty(sb, field, options);
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// サブテーブルクラスを出力する    
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="field"></param>
    /// <param name="options"></param>
    private void EmitSubtableClass(StringBuilder sb, KintoneFieldMetadata field, CodeEmitterOptions options) {
        var className = this._nameConverter.ToClassName(field.Label, field.Code);

        sb.AppendLine($"public {(options.UseRecord ? "record" : "class")} {className}");
        sb.AppendLine("{");

        foreach (var sub in field.SubFields!)
        {
            this.EmitProperty(sb, sub, options);
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// プロパティを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="field"></param>
    /// <param name="options"></param>
    private void EmitProperty(StringBuilder sb, KintoneFieldMetadata field, CodeEmitterOptions options) {
        var propName = this._nameConverter.ToPropertyName(field.Label, field.Code);
        var typeName = this._typeMapper.Map(field);

        // ★ サブテーブル型名の補正
        if (field.Type == KintoneFieldType.SubTable) {
            var className = this._nameConverter.ToClassName(field.Label, field.Code);
            typeName = $"List<{className}>";
        }

        sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
    }
}
