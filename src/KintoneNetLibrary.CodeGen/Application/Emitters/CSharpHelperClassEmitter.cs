using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Models;
using KintoneNetLibrary.CodeGen.Domain.Options;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

public class CSharpHelperClassEmitter : IHelperClassEmitter {
    private readonly string _baseTemplate;
    private readonly string _derivedTemplate;

    public CSharpHelperClassEmitter() {
        // テンプレートは埋め込みリソース or ファイル読み込み
        this._baseTemplate = ReadTemplate("EntityInfoBaseTemplate.txt");
        this._derivedTemplate = ReadTemplate("EntityInfoDerivedTemplate.txt");
    }

    public IEnumerable<GeneratedHelperClass> EmitHelperClasses(CodeEmitterOptions options) {
        var list = new List<GeneratedHelperClass> {
            // 1. Base クラス
            new() {
                ClassName = "KintoneEntityInfo",
                Code = this._baseTemplate.Replace("{{Namespace}}", options.Namespace)
            },

            // 2. UserInfo
            this.EmitDerived("UserInfo", "ユーザー情報", options),

            // 3. GroupInfo
            this.EmitDerived("GroupInfo", "グループ情報", options),

            // 4. OrganizationInfo
            this.EmitDerived("OrganizationInfo", "組織情報", options)
        };

        return list;
    }

    private GeneratedHelperClass EmitDerived(string className, string summary, CodeEmitterOptions options) {
        var code = this._derivedTemplate
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