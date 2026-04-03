using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// KintoneフィールドのメタデータをJSONから解析するためのインターフェース
/// </summary>
public interface IKintoneFieldParser {
    /// <summary>
    /// JSONのプロパティからKintoneフィールドのメタデータを解析してリストとして返します。
    /// </summary>
    /// <param name="properties">Kintoneフィールドのプロパティを含むJSON要素</param>
    /// <returns>Kintoneフィールドのメタデータのリスト</returns>
    List<KintoneFieldMetadata> Parse(JsonElement properties);
}