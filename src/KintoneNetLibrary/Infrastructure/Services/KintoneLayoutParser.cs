using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Services;

/// <summary>
/// KintoneアプリのレイアウトJSONを解析して、レイアウトメタデータを取得するための実装クラス
/// </summary>
public class KintoneLayoutParser : IKintoneLayoutParser {
    /// <summary>
    /// KintoneアプリのレイアウトJSONを解析して、レイアウトメタデータを取得します。
    /// </summary>
    /// <param name="properties">レイアウトJSONのルート要素</param>
    /// <returns>レイアウトメタデータ</returns>
    public KintoneLayoutMetadata Parse(JsonElement properties) {
        var revisionString = properties.GetProperty("revision").GetString();
        if (!int.TryParse(revisionString, out var revision)) {
            throw new InvalidOperationException($"レイアウトJSONのrevisionプロパティが整数に変換できませんでした。値: {revisionString}");
        }
        var metadata = new KintoneLayoutMetadata {
            Revision = revision,
            Layout = this.ParseLayoutArray(properties.GetProperty("layout"))
        };

        return metadata;
    }

    /// <summary>
    /// レイアウトJSONの配列を解析して、レイアウトブロックのリストを取得します。
    /// レイアウトブロックはROW、SUBTABLE、GROUPのいずれかになります。
    /// フィールドはコードとタイプのみを保持し、詳細なフィールド情報は含まれません。
    /// 想定外のタイプがあった場合は無視されます。
    /// </summary>
    /// <param name="layoutArray">レイアウトJSONの配列要素</param>
    /// <returns>レイアウトブロックのリスト</returns>
    /// <remarks>
    /// - ROW: フィールドの配列を持ちます。フィールドはコードとタイプのみを保持します。
    /// - SUBTABLE: コードとフィールドの配列を持ちます。フィールドはコードとタイプのみを保持します。
    /// - GROUP: コードとレイアウトブロックの配列を持ちます。レイアウトブロックは再帰的に同じ構造になります。
    /// </remarks>
    private List<KintoneLayoutBlock> ParseLayoutArray(JsonElement layoutArray) {
        var list = new List<KintoneLayoutBlock>();

        foreach (var element in layoutArray.EnumerateArray()) {
            var type = element.GetProperty("type").GetString();

            switch (type) {
                case "ROW":
                    list.Add(ParseRow(element));
                    break;

                case "SUBTABLE":
                    list.Add(ParseSubTable(element));
                    break;

                case "GROUP":
                    list.Add(ParseGroup(element));
                    break;

                default:
                    // 想定外のタイプは無視
                    break;
            }
        }

        return list;
    }

    /// <summary>
    /// ROWタイプのレイアウトブロックを解析して、KintoneLayoutRowオブジェクトを作成します。
    /// </summary>
    /// <param name="element">ROWタイプのレイアウトブロックのJSON要素</param>
    /// <returns>解析されたKintoneLayoutRowオブジェクト</returns>
    private static KintoneLayoutRow ParseRow(JsonElement element) {
        var row = new KintoneLayoutRow { Type = "ROW" };

        if (element.TryGetProperty("fields", out var fields)) {
            foreach (var f in fields.EnumerateArray()) {
                row.Fields.Add(ParseField(f));
            }
        }

        return row;
    }

    /// <summary>
    /// SUBTABLEタイプのレイアウトブロックを解析して、KintoneLayoutSubTableオブジェクトを作成します。
    /// </summary>
    /// <param name="element">SUBTABLEタイプのレイアウトブロックのJSON要素</param>
    /// <returns>解析されたKintoneLayoutSubTableオブジェクト</returns>
    private static KintoneLayoutSubTable ParseSubTable(JsonElement element) {
        var sub = new KintoneLayoutSubTable {
            Type = "SUBTABLE",
            Code = element.GetProperty("code").GetString() ?? ""
        };

        if (element.TryGetProperty("fields", out var fields)) {
            foreach (var f in fields.EnumerateArray()) {
                sub.Fields.Add(ParseField(f));
            }
        }

        return sub;
    }

    /// <summary>
    /// GROUPタイプのレイアウトブロックを解析して、KintoneLayoutGroupオブジェクトを作成します。
    /// </summary>
    /// <param name="element">GROUPタイプのレイアウトブロックのJSON要素</param>
    /// <returns>解析されたKintoneLayoutGroupオブジェクト</returns>
    private KintoneLayoutGroup ParseGroup(JsonElement element) {
        var group = new KintoneLayoutGroup {
            Type = "GROUP",
            Code = element.GetProperty("code").GetString() ?? ""
        };

        if (element.TryGetProperty("layout", out var layout)) {
            group.Layout = this.ParseLayoutArray(layout);
        }

        return group;
    }

    /// <summary>
    /// フィールドのJSON要素を解析して、KintoneLayoutFieldオブジェクトを作成します。
    /// </summary>
    /// <param name="element">フィールドのJSON要素</param>
    /// <returns>解析されたKintoneLayoutFieldオブジェクト</returns>
    private static KintoneLayoutField ParseField(JsonElement element) {
        var field = new KintoneLayoutField {
            Type = element.GetProperty("type").GetString() ?? ""
        };

        // LABEL / SPACER / HR は code がない
        if (element.TryGetProperty("code", out var codeProp)) {
            field.Code = codeProp.GetString();
        }

        return field;
    }
}
