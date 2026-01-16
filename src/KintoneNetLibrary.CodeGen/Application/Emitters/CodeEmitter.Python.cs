using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// Python コードエミッタ
/// </summary>
/// <param name="converterFactory"></param>
/// <param name="mapperFactory"></param>
public class PythonCodeEmitter(
    INameConverterFactory converterFactory,
    ITypeMapperFactory mapperFactory,
    ILogger<PythonCodeEmitter>? logger = null) : ICodeEmitter {
    private readonly INameConverter _converter = converterFactory.Create(GenerateLanguages.Python);
    private readonly ITypeMapper _mapper = mapperFactory.Create(GenerateLanguages.Python);
    private readonly ILogger<PythonCodeEmitter>? _logger = logger;

    /// <summary>
    /// 対応する生成言語
    /// </summary>
    public GenerateLanguages Language => GenerateLanguages.Python;

    /// <summary>
    /// コード生成を実行します
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options) {
        var pyOptions = (PythonEmitterOptions)options;
        var sb = new StringBuilder();

        // ヘッダ
        if (!string.IsNullOrWhiteSpace(pyOptions.HeaderComment)) {
            sb.AppendLine($"# {pyOptions.HeaderComment}");
        }
        sb.AppendLine($"# module: {pyOptions.ModuleName}");
        sb.AppendLine($"# AppId: {schema.AppId}");
        sb.AppendLine($"# Revision: {schema.Revision}");
        sb.AppendLine();

        // メインクラス
        this.EmitMainClass(sb, schema, pyOptions);

        // サブテーブル
        foreach (var subTable in schema.SubTables) {
            this.EmitSubTable(sb, subTable, pyOptions);
        }

        return new GeneratedModelResult {
            MainModelCode = sb.ToString(),
            Revision = schema.Revision
        };
    }

    /// <summary>
    /// メインクラスを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    private void EmitMainClass(StringBuilder sb, KintoneAppSchema schema, PythonEmitterOptions options) {
        var className = this._converter.ToClassName(schema.AppName, "");
        sb.AppendLine($"class {className}:");

        if (schema.Fields.Count == 0) {
            sb.AppendLine("    pass");
            sb.AppendLine();
            return;
        }

        foreach (var field in schema.Fields) {
            var propName = this._converter.ToPropertyName(field.Label, field.FieldCode);
            var typeName = this._mapper.MapType(field, options.UseTypeHint);

            sb.AppendLine($"    {propName}: {typeName}");
        }

        sb.AppendLine();
    }

    /// <summary>
    /// サブテーブルを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="subTable"></param>
    /// <param name="options"></param>
    private void EmitSubTable(StringBuilder sb, KintoneSubTableSchema subTable, PythonEmitterOptions options) {
        var className = this._converter.ToClassName(
            string.IsNullOrWhiteSpace(subTable.Label) ? subTable.FieldCode : subTable.Label,
            subTable.FieldCode
        );

        sb.AppendLine($"class SubTable{className}:");

        if (subTable.Fields.Count == 0) {
            sb.AppendLine("    pass");
            sb.AppendLine();
            return;
        }

        foreach (var field in subTable.Fields) {
            var propName = this._converter.ToPropertyName(field.Label, field.FieldCode);
            var typeName = this._mapper.MapType(field, options.UseTypeHint);

            sb.AppendLine($"    {propName}: {typeName}");
        }

        sb.AppendLine();
    }
}
