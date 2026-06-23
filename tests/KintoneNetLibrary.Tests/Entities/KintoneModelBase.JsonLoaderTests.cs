using System.Text.Json;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

/// <summary>
/// KintoneModelBaseのJSONロード機能のテストクラス。
/// </summary>
public class KintoneModelBaseJsonLoaderTests {
    /// <summary>
    /// テスト用ダミーモデル。
    /// </summary>
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppId { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;

        [KintoneItem(fieldCode: "FieldB", fieldType: KintoneFieldType.Number)]
        public int FieldB { get; set; }

        [KintoneItem(fieldCode: "FieldC", fieldType: KintoneFieldType.SingleLineText, isDownload: false)]
        public string FieldC { get; set; } = string.Empty;
    }

    /// <summary>
    /// テスト用サブテーブル行モデル。
    /// </summary>
    public class DummySubTableItem : KintoneSubTableBase {
        [KintoneItem(fieldCode: "SubFieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string SubFieldA { get; set; } = string.Empty;
    }

    /// <summary>
    /// サブテーブルを含むテスト用ダミーモデル。
    /// </summary>
    public class DummyModelWithSubTable : KintoneModelBase<DummyModelWithSubTable> {
        public override int AppId { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "FieldA", fieldType: KintoneFieldType.SingleLineText)]
        public string FieldA { get; set; } = string.Empty;

        [KintoneItem(fieldCode: "SubTable", fieldType: KintoneFieldType.SubTable)]
        public List<DummySubTableItem> SubTable { get; set; } = [];
    }

    /// <summary>
    /// フィールドマップを文字列から生成するヘルパー。
    /// </summary>
    private static Dictionary<string, JsonElement> ParseFieldMap(string json) {
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
    }

    #region <<Test methods>>
    /// <summary>
    /// 通常フィールドが正しくプロパティにマッピングされることをテストする。
    /// </summary>
    [Fact]
    public void LoadFromJsonDictionary_通常フィールドが正しく読み込まれる() {
        // Arrange
        var fieldMap = ParseFieldMap("""
            {
                "FieldA": { "value": "テスト値" },
                "FieldB": { "value": "42" }
            }
            """);
        var model = new DummyModel();

        // Act
        model.LoadFromJsonDictionary(fieldMap);

        // Assert
        Assert.Equal("テスト値", model.FieldA);
        Assert.Equal(42, model.FieldB);
    }

    /// <summary>
    /// $idフィールドがRecordIdプロパティに正しくマッピングされることをテストする。
    /// </summary>
    [Fact]
    public void LoadFromJsonDictionary_RecordIdが正しく読み込まれる() {
        // Arrange
        var fieldMap = ParseFieldMap("""
            {
                "$id": { "value": "123" },
                "FieldA": { "value": "テスト値" }
            }
            """);
        var model = new DummyModel();

        // Act
        model.LoadFromJsonDictionary(fieldMap);

        // Assert
        Assert.Equal("123", model.RecordId);
    }

    /// <summary>
    /// IsDownload=falseのフィールドが読み込まれないことをテストする。
    /// </summary>
    [Fact]
    public void LoadFromJsonDictionary_IsDownloadFalseのフィールドはスキップされる() {
        // Arrange
        var fieldMap = ParseFieldMap("""
            {
                "FieldA": { "value": "テスト値" },
                "FieldC": { "value": "読み込まれない値" }
            }
            """);
        var model = new DummyModel();

        // Act
        model.LoadFromJsonDictionary(fieldMap);

        // Assert
        Assert.Equal("テスト値", model.FieldA);
        Assert.Equal(string.Empty, model.FieldC);
    }

    /// <summary>
    /// フィールドマップに存在しないフィールドはプロパティの初期値が保持されることをテストする。
    /// </summary>
    [Fact]
    public void LoadFromJsonDictionary_フィールドマップに存在しないフィールドはスキップされる() {
        // Arrange
        var fieldMap = ParseFieldMap("""
            {
                "FieldA": { "value": "テスト値" }
            }
            """);
        var model = new DummyModel { FieldB = 99 };

        // Act
        model.LoadFromJsonDictionary(fieldMap);

        // Assert
        Assert.Equal("テスト値", model.FieldA);
        Assert.Equal(99, model.FieldB);
    }

    /// <summary>
    /// サブテーブルが正しくリストとして読み込まれることをテストする。
    /// </summary>
    [Fact]
    public void LoadFromJsonDictionary_サブテーブルが正しく読み込まれる() {
        // Arrange
        var fieldMap = ParseFieldMap("""
            {
                "FieldA": { "value": "テスト値" },
                "SubTable": {
                    "value": [
                        { "id": "1", "value": { "SubFieldA": { "value": "行1" } } },
                        { "id": "2", "value": { "SubFieldA": { "value": "行2" } } }
                    ]
                }
            }
            """);
        var model = new DummyModelWithSubTable();

        // Act
        model.LoadFromJsonDictionary(fieldMap);

        // Assert
        Assert.Equal("テスト値", model.FieldA);
        Assert.Equal(2, model.SubTable.Count);
        Assert.Equal("行1", model.SubTable[0].SubFieldA);
        Assert.Equal("行2", model.SubTable[1].SubFieldA);
    }
    #endregion
}
