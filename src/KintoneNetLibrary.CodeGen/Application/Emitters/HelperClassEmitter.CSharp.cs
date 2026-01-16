using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// C# ヘルパークラスエミッター
/// </summary>
/// <remarks>
/// コンストラクター
/// </remarks>
/// <param name="logger"></param>
public class CSharpHelperClassEmitter(ILogger<CSharpHelperClassEmitter> logger) : IHelperClassEmitter {
    private readonly string _baseTemplate = ReadTemplate("EntityInfoBaseTemplate.txt");
    private readonly string _derivedTemplate = ReadTemplate("EntityInfoDerivedTemplate.txt");
    private readonly string _fileInfoTemplate = ReadTemplate("EntityFileInfoTemplate.txt");
    private readonly ILogger<CSharpHelperClassEmitter> _logger = logger;

    /// <summary>
    /// ヘルパークラス群を生成する
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) {
        var helperOptions = options as CSharpEmitterOptions
            ?? throw new InvalidOperationException("Invalid options type.");
        var list = new List<GeneratedHelperClass> {
            // 1. Base クラス
            new() {
                ClassName = "KintoneEntityInfo",
                Code = this._baseTemplate.Replace("{{Namespace}}", helperOptions.Namespace)
            },

            // 2. UserInfo
            this.EmitFromTemplate(this._derivedTemplate, "UserInfo", "ユーザー情報", helperOptions),

            // 3. GroupInfo
            this.EmitFromTemplate(this._derivedTemplate, "GroupInfo", "グループ情報", helperOptions),

            // 4. OrganizationInfo
            this.EmitFromTemplate(this._derivedTemplate, "OrganizationInfo", "組織情報", helperOptions),

            // 5. FileInfo
            this.EmitFromTemplate(this._fileInfoTemplate, "FileInfo", "ファイル情報", helperOptions)
        };

        return list;
    }

    /// <summary>
    /// テンプレートからヘルパークラスを生成する
    /// </summary>
    /// <param name="template"></param>
    /// <param name="className"></param>
    /// <param name="summary"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private GeneratedHelperClass EmitFromTemplate(string template, string className, string summary, CSharpEmitterOptions options) {
        var code = template
            .Replace("{{Namespace}}", options.Namespace)
            .Replace("{{ClassName}}", className)
            .Replace("{{Summary}}", summary);

        return new GeneratedHelperClass {
            ClassName = className,
            Code = code
        };
    }

    /// <summary>
    /// テンプレートを読み込む
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static string ReadTemplate(string fileName) {
        var assembly = typeof(CSharpHelperClassEmitter).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .First(n => n.EndsWith(fileName));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Template not found: {fileName}");

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}