using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

public class CSharpHelperClassEmitter : IHelperClassEmitter {
    private readonly string _baseTemplate;
    private readonly string _derivedTemplate;
    private readonly string _fileInfoTemplate;

    public CSharpHelperClassEmitter() {
        // テンプレートは埋め込みリソース or ファイル読み込み
        this._baseTemplate = ReadTemplate("EntityInfoBaseTemplate.txt");
        this._derivedTemplate = ReadTemplate("EntityInfoDerivedTemplate.txt");
        this._fileInfoTemplate = ReadTemplate("EntityFileInfoTemplate.txt");
    }

    public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) {
        var list = new List<GeneratedHelperClass> {
            // 1. Base クラス
            new() {
                ClassName = "KintoneEntityInfo",
                Code = this._baseTemplate.Replace("{{Namespace}}", options.Namespace)
            },

            // 2. UserInfo
            this.EmitFromTemplate(this._derivedTemplate, "UserInfo", "ユーザー情報", options),

            // 3. GroupInfo
            this.EmitFromTemplate(this._derivedTemplate, "GroupInfo", "グループ情報", options),

            // 4. OrganizationInfo
            this.EmitFromTemplate(this._derivedTemplate, "OrganizationInfo", "組織情報", options),

            // 5. FileInfo
            this.EmitFromTemplate(this._fileInfoTemplate, "FileInfo", "ファイル情報", options)
        };

        return list;
    }

    private GeneratedHelperClass EmitFromTemplate(string template, string className, string summary, CodeEmitterOptions options) {
        var code = template
            .Replace("{{Namespace}}", options.Namespace)
            .Replace("{{ClassName}}", className)
            .Replace("{{Summary}}", summary);

        return new GeneratedHelperClass {
            ClassName = className,
            Code = code
        };
    }

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