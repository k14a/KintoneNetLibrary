using System.Text.Json;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

/// <summary>
/// Kintoneアプリのスキーマ情報から、コード生成に必要なメタデータを読み込むサービス実装
/// </summary>
/// <param name="fieldParser">フィールドパーサー</param>
/// <param name="layoutParser">レイアウトパーサー</param>
public class MetadataLoader(IKintoneFieldParser fieldParser, IKintoneLayoutParser layoutParser) : IMetadataLoader {
    private readonly IKintoneFieldParser _fieldParser = fieldParser;
    private readonly IKintoneLayoutParser _layoutParser = layoutParser;

    /// <summary>
    /// Kintoneアプリのスキーマ情報から、コード生成に必要なメタデータを読み込みます。
    /// </summary>
    /// <param name="fieldsJsonPath">fields.json のパス</param>
    /// <param name="layoutJsonPath">layout.json のパス</param>
    /// <returns>読み込んだメタデータ</returns>
    public async Task<KintoneAppMetadata> LoadAsync(string fieldsJsonPath, string layoutJsonPath) {
        // -----------------------------
        // 1. fields.json を読み込む
        // -----------------------------
        using var fieldsStream = File.OpenRead(fieldsJsonPath);
        using var fieldsDoc = await JsonDocument.ParseAsync(fieldsStream);

        var fieldsRoot = fieldsDoc.RootElement;

        var revision = fieldsRoot.GetProperty("revision").GetString();
        int.TryParse(revision, out var rev);

        var properties = fieldsRoot.GetProperty("properties");

        // ★ ここが重要：JsonElement をそのまま渡す
        var fieldMetadataList = this._fieldParser.Parse(properties);

        // -----------------------------
        // 2. layout.json を読み込む
        // -----------------------------
        using var layoutStream = File.OpenRead(layoutJsonPath);
        using var layoutDoc = await JsonDocument.ParseAsync(layoutStream);

        var layoutMetadata = this._layoutParser.Parse(layoutDoc.RootElement);

        // -----------------------------
        // 3. KintoneAppMetadata に統合
        // -----------------------------
        return new KintoneAppMetadata {
            AppId = 0, // fields.json には AppId がないので後で補完する
            Revision = rev,
            Fields = fieldMetadataList,
            Layout = layoutMetadata
        };
    }
}
