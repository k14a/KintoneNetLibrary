using System.Text.Json;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.CodeGen.Infrastructure.Services;

public class MetadataLoader(IKintoneFieldParser fieldParser, IKintoneLayoutParser layoutParser) : IMetadataLoader {
    private readonly IKintoneFieldParser _fieldParser = fieldParser;
    private readonly IKintoneLayoutParser _layoutParser = layoutParser;

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
