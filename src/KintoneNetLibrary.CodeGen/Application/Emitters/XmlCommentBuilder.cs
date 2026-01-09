using System.Text;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Domain.Schemas;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.CodeGen.Application.Emitters;

/// <summary>
/// XML コメント生成器
/// </summary>
public class XmlCommentBuilder : IXmlCommentBuilder {
    /// <summary>
    /// フィールド用 XML コメントを生成する
    /// </summary>
    public string BuildForField(KintoneFieldSchema field) {
        var sb = new StringBuilder();

        sb.AppendLine("/// <summary>");

        // Label があれば summary に入れる
        if (!string.IsNullOrWhiteSpace(field.Label)) {
            sb.AppendLine($"/// {Escape(field.Label)}");
        } else {
            sb.AppendLine("/// Kintone field");
        }

        sb.AppendLine("/// </summary>");

        // Calc フィールドは注意喚起
        if (field.FieldType == KintoneFieldType.Calc) {
            sb.AppendLine("/// <remarks>");
            sb.AppendLine("/// Calc フィールドは計算式の結果が数値・文字列・日付など多様な型になるため、string として生成されています。");
            sb.AppendLine("/// 必要に応じてユーザー側で適切な型に変更してください。");
            sb.AppendLine("/// </remarks>");
        }

        // 選択肢がある場合は values に列挙
        if (field.Options?.Count > 0) {
            sb.AppendLine("/// <values>");
            sb.AppendLine("/// 選択肢:");
            foreach (var opt in field.Options) {
                sb.AppendLine($"/// - {Escape(opt)}");
            }

            sb.AppendLine("/// </values>");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// サブテーブル用 XML コメントを生成する
    /// </summary>
    public string BuildForSubTable(KintoneSubTableSchema subTable) {
        var sb = new StringBuilder();

        sb.AppendLine("/// <summary>");

        if (!string.IsNullOrWhiteSpace(subTable.Label)) {
            sb.AppendLine($"/// サブテーブル: {Escape(subTable.Label)}");
        } else {
            sb.AppendLine("/// サブテーブル");
        }

        sb.AppendLine("/// </summary>");

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// XML コメント内で不正な文字をエスケープ
    /// </summary>
    private static string Escape(string text) {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}