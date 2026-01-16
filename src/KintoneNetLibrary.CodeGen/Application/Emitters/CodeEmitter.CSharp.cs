using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// C# コードエミッター
/// </summary>
/// <param name="converterFactory"></param>
/// <param name="mapperFactory"></param>
/// <param name="xml"></param>
/// <param name="subTableEmitter"></param>
/// <param name="helperEmitter"></param>
public class CSharpCodeEmitter(
    INameConverterFactory converterFactory,
    ITypeMapperFactory mapperFactory,
    IXmlCommentBuilder xml,
    ISubTableEmitter subTableEmitter,
    IHelperClassEmitter helperEmitter,
    ILogger<CSharpCodeEmitter>? logger = null) : ICodeEmitter {

    private readonly INameConverter _converter = converterFactory.Create(GenerateLanguages.CSharp);
    private readonly ITypeMapper _mapper = mapperFactory.Create(GenerateLanguages.CSharp);
    private readonly IXmlCommentBuilder _xml = xml;
    private readonly ISubTableEmitter _subTableEmitter = subTableEmitter;
    private readonly IHelperClassEmitter _helperEmitter = helperEmitter;
    private readonly ILogger<CSharpCodeEmitter>? _logger = logger;

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

    /// <summary>
    /// 対応する生成言語
    /// </summary>
    public GenerateLanguages Language => GenerateLanguages.CSharp;

    /// <summary>
    /// システムフィールドかどうかを判定する
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    private static bool IsSystemField(KintoneFieldSchema field) {
        return SystemFieldCodes.Contains(field.FieldCode);
    }

    /// <summary>
    /// Kintone アプリスキーマから C# コードを生成する
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public GeneratedModelResult Emit(KintoneAppSchema schema, CodeEmitterOptions options) {
        var csOptions = (CSharpEmitterOptions)options;
        var result = new GeneratedModelResult {
            // 1. メインモデル生成
            MainModelCode = this.EmitMainModel(schema, csOptions),
            Revision = schema.Revision
        };

        // 2. サブテーブル生成
        foreach (var sub in schema.SubTables) {
            result.SubTableModels.Add(
                this._subTableEmitter.EmitSubTable(sub.FieldCode, sub, csOptions)
            );
        }

        // 3. pure モードなら補助クラス生成
        if (!csOptions.UseKintoneNetLibrary) {
            result.HelperClasses.AddRange(
                this._helperEmitter.EmitHelperClasses(csOptions)
            );
        }

        return result;
    }

    /// <summary>
    /// メインモデルを生成する
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private string EmitMainModel(KintoneAppSchema schema, CSharpEmitterOptions options) {
        var sb = new StringBuilder();

        // ヘッダーコメント
        this.EmitHeaderComment(sb, schema, options);

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

    /// <summary>
    /// ヘッダーコメントを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    private void EmitHeaderComment(StringBuilder sb, KintoneAppSchema schema, CSharpEmitterOptions options) {
        if (!string.IsNullOrWhiteSpace(options.HeaderComment)) {
            sb.AppendLine($"// {options.HeaderComment}");
            sb.AppendLine($"// AppId: {schema.AppId}");
            sb.AppendLine($"// Revision: {schema.Revision}");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// using セクションを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="options"></param>
    private void EmitUsingSection(StringBuilder sb, CSharpEmitterOptions options) {
        sb.AppendLine("using System;");
        if (options.UseKintoneNetLibrary) {
            sb.AppendLine("using KintoneNetLibrary.Domain.Entities;");
            sb.AppendLine("using KintoneNetLibrary.Domain.Access;");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// クラス名を出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    private void EmitClassName(StringBuilder sb, KintoneAppSchema schema, CSharpEmitterOptions options) {
        var className = options.MainClassName;
        var keyword = options.UseRecord ? "Record" : "class";
        var partial = options.UsePartial ? "partial " : "";
        sb.Append($"public {partial}{keyword} {className}");
        if (options.UseKintoneNetLibrary) {
            sb.Append(" : KintoneModelBase<" + className + ">");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// プロパティを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="field"></param>
    /// <param name="options"></param>
    private void EmitProperty(StringBuilder sb, KintoneFieldSchema field, CSharpEmitterOptions options) {
        var propName = this._converter.ToPropertyName(field.Label, field.FieldCode);
        var typeName = this._mapper.MapType(field, options.UseKintoneNetLibrary);

        // 変換失敗（"_"）を検知
        if (propName == "_") {
            // ビルドエラーを確実に起こす名前に置換
            propName = $"__PropertyNameConversionFailed_{field.FieldCode}__";

            // TODO コメントを挿入
            sb.AppendLine("    // TODO: プロパティ名の変換に失敗しました。修正してください。");

            // ログ出力
            this._logger.LogWarning("プロパティの生成に失敗しました。ラベル：{label}/フィールドコード：{fieldCode}", field.Label, field.FieldCode);
        }

        // XML コメント
        var xml = this._xml.BuildForField(field);
        sb.AppendLine(xml);

        // KintoneItemAttribute
        this.EmitAttributes(sb, field, options);

        // プロパティ本体
        sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
        sb.AppendLine();
    }

    /// <summary>
    /// サブテーブルを出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="schema"></param>
    /// <param name="options"></param>
    private void EmitSubTables(StringBuilder sb, KintoneSubTableSchema schema, CSharpEmitterOptions options) {
        var propName = this._converter.ToPropertyName(schema.Label, schema.FieldCode);
        var classNameSub = this._converter.ToClassName(schema.Label, schema.FieldCode);

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

    /// <summary>
    /// 属性を出力する
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="field"></param>
    /// <param name="options"></param>
    private void EmitAttributes(StringBuilder sb, KintoneFieldSchema field, CSharpEmitterOptions options) {
        if (options.UseKintoneNetLibrary) {
            sb.AppendLine($"    [KintoneItem(FieldCode = \"{field.FieldCode}\")]");
        }
    }
}
