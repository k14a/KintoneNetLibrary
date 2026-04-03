using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// KintoneアプリのレイアウトJSONを解析して、レイアウトメタデータを取得するためのインターフェース
/// </summary>
public interface IKintoneLayoutParser {
    /// <summary>
    /// KintoneアプリのレイアウトJSONを解析して、レイアウトメタデータを取得します。
    /// </summary>
    /// <param name="properties">KintoneアプリのレイアウトJSONのルート要素</param>
    /// <returns>解析されたレイアウトメタデータ</returns>
    KintoneLayoutMetadata Parse(JsonElement properties);
}