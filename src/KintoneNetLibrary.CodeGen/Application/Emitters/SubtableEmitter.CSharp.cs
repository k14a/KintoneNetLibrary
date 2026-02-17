using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Enums;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// C# サブテーブルエミッター
/// </summary>
/// <param name="converterFactory">名前変換ファクトリ</param>
/// <param name="mapperFactory">型マッパーファクトリ</param>
/// <param name="xml">XML コメントビルダー</param>
/// <param name="logger">ロガー</param>
public class CSharpSubTableEmitter(
    INameConverterFactory converterFactory,
    ITypeMapperFactory mapperFactory,
    IXmlCommentBuilder xml,
    ILogger<CSharpSubTableEmitter> logger) : ISubTableEmitter {

    private readonly INameConverter _converter = converterFactory.Create(GenerateLanguages.CSharp);
    private readonly ITypeMapper _types = mapperFactory.Create(GenerateLanguages.CSharp);
    private readonly IXmlCommentBuilder _xml = xml;
    private readonly ILogger<CSharpSubTableEmitter> _logger = logger;
    private readonly HashSet<string> _generatedClassNames = [];

    /// <summary>
    /// サブテーブルモデルを生成する
    /// </summary>
    /// <param name="name">サブテーブル名</param>
    /// <param name="subTable">サブテーブルスキーマ</param>
    /// <param name="options">エミッターオプション</param>
    /// <returns>生成されたサブテーブルモデル</returns>
    public GeneratedSubTableModel EmitSubTable(string name, KintoneSubTableSchema subTable, CSharpEmitterOptions options) {
        var sb = new StringBuilder();

        this.EmitUsingSection(sb, options);

        // namespace
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();

        // class name
        name = this._converter.ToClassName(name, string.Empty);
        name = this.MakeUniqueClassName(name);
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

    /// <summary>
    /// 一意なクラス名を生成する
    /// </summary>
    /// <param name="baseName">基底となるクラス名</param>
    /// <returns>一意なクラス名</returns>
    private string MakeUniqueClassName(string baseName) {
        var className = baseName;
        var index = 1;
        while (this._generatedClassNames.Contains(className)) {
            className = $"{baseName}{index}";
            index++;
        }
        this._generatedClassNames.Add(className);
        return className;
    }

    /// <summary>
    /// using セクションを出力する
    /// </summary>
    /// <param name="sb">文字列ビルダー</param>
    /// <param name="options">エミッターオプション</param>
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
    /// <param name="sb">文字列ビルダー</param>
    /// <param name="className">クラス名</param>
    /// <param name="options">エミッターオプション</param>
    private void EmitClassName(StringBuilder sb, string className, CSharpEmitterOptions options) {
        sb.Append($"public partial class {className}");
        if (options.UseKintoneNetLibrary) {
            sb.Append(" : KintoneSubTableBase");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// プロパティを出力する
    /// </summary>
    /// <param name="sb">文字列ビルダー</param>
    /// <param name="field">フィールドスキーマ</param>
    /// <param name="options">エミッターオプション</param>
    private void EmitProperty(StringBuilder sb, KintoneFieldSchema field, CSharpEmitterOptions options) {
        var propName = this._converter.ToPropertyName(field.Label, field.FieldCode);
        var typeName = this._types.MapType(field, options.UseKintoneNetLibrary);

        // XML コメント
        sb.AppendLine(this._xml.BuildForField(field));

        // KintoneItemAttribute
        this.EmitAttributes(sb, field, options);

        sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
        sb.AppendLine();
    }

    /// <summary>
    /// 属性を出力する
    /// </summary>
    /// <param name="sb">文字列ビルダー</param>
    /// <param name="field">フィールドスキーマ</param>
    /// <param name="options">エミッターオプション</param>
    private void EmitAttributes(StringBuilder sb, KintoneFieldSchema field, CSharpEmitterOptions options) {
        if (options.UseKintoneNetLibrary) {
            sb.AppendLine($"    [KintoneItem(FieldCode = \"{field.FieldCode}\")]");
        }
    }
}