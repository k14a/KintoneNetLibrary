using System.Text.Json;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using Xunit;

namespace KintoneNetLibrary.Tests.Entities;

/// <summary>
/// KintoneModelBaseのレコード生成機能（ClearIfNull）のテストクラス。
/// </summary>
public class KintoneModelBaseRecordBuilderTests {
    /// <summary>
    /// テスト用ダミーモデル。
    /// </summary>
    public class DummyModel : KintoneModelBase<DummyModel> {
        public override int AppId { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "Number", fieldType: KintoneFieldType.Number)]
        public int? Number { get; set; }

        [KintoneItem(fieldCode: "NumberClearable", fieldType: KintoneFieldType.Number, ClearIfNull = true)]
        public int? NumberClearable { get; set; }

        [KintoneItem(fieldCode: "MultiSelectClearable", fieldType: KintoneFieldType.MultiSelect, ClearIfNull = true)]
        public IEnumerable<string>? MultiSelectClearable { get; set; }

        [KintoneItem(fieldCode: "SubTableUploadable", fieldType: KintoneFieldType.SubTable)]
        public IEnumerable<DummySubTableItem>? SubTableUploadable { get; set; }

        [KintoneItem(fieldCode: "SubTableNotUploadable", fieldType: KintoneFieldType.SubTable, IsUpload = false)]
        public IEnumerable<DummySubTableItem>? SubTableNotUploadable { get; set; }
    }

    /// <summary>
    /// サブテーブルの行を表すダミークラス。
    /// </summary>
    public class DummySubTableItem : KintoneSubTableBase {
        [KintoneItem(fieldCode: "Text", fieldType: KintoneFieldType.SingleLineText)]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// IsRequired と ClearIfNull を同時に指定した矛盾テスト用のダミーモデル。
    /// </summary>
    public class InvalidDummyModel : KintoneModelBase<InvalidDummyModel> {
        public override int AppId { get; init; }
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("DummyDomain", "DummyApiToken");

        [KintoneItem(fieldCode: "RequiredClearable", fieldType: KintoneFieldType.Number, ClearIfNull = true, IsRequired = true)]
        public int? RequiredClearable { get; set; }
    }

    /// <summary>
    /// フィールドの JSON 文字列を取得するヘルパー。
    /// </summary>
    private static string SerializeField(DummyModel model, string fieldCode) {
        var record = model.ToKintoneRecord();
        return JsonSerializer.Serialize(record[fieldCode], DefaultJsonOptions.Default);
    }

    #region <<Test methods>>
    /// <summary>
    /// ClearIfNull が false（デフォルト）の場合、null 値は value キーごと省略される（既存挙動を維持）。
    /// </summary>
    [Fact]
    public void ToKintoneRecordWhenValueIsNullAndClearIfNullFalseOmitsValueKey() {
        var model = new DummyModel { Number = null };

        var json = SerializeField(model, "Number");

        Assert.Equal("{}", json);
    }

    /// <summary>
    /// ClearIfNull が true の場合、null 値は空文字列としてクリア送信される。
    /// </summary>
    [Fact]
    public void ToKintoneRecordWhenValueIsNullAndClearIfNullTrueSendsEmptyString() {
        var model = new DummyModel { NumberClearable = null };

        var json = SerializeField(model, "NumberClearable");

        Assert.Equal("{\"value\":\"\"}", json);
    }

    /// <summary>
    /// ClearIfNull が true でも値が設定されていれば通常通り送信される。
    /// </summary>
    [Fact]
    public void ToKintoneRecordWhenValueIsNotNullAndClearIfNullTrueSendsActualValue() {
        var model = new DummyModel { NumberClearable = 123 };

        var json = SerializeField(model, "NumberClearable");

        Assert.Equal("{\"value\":123}", json);
    }

    /// <summary>
    /// 複数値フィールドで ClearIfNull が true かつ null の場合、空配列としてクリア送信される。
    /// </summary>
    [Fact]
    public void ToKintoneRecordWhenMultiValueFieldIsNullAndClearIfNullTrueSendsEmptyArray() {
        var model = new DummyModel { MultiSelectClearable = null };

        var json = SerializeField(model, "MultiSelectClearable");

        Assert.Equal("{\"value\":[]}", json);
    }

    /// <summary>
    /// IsRequired と ClearIfNull を同時に指定した場合は矛盾した属性定義として例外がスローされる。
    /// </summary>
    [Fact]
    public void ToKintoneRecordWhenIsRequiredAndClearIfNullBothTrueThrows() {
        var model = new InvalidDummyModel();

        Assert.Throws<InvalidOperationException>(() => model.ToKintoneRecord());
    }

    /// <summary>
    /// サブテーブルが IsUpload = false の場合、ToKintoneRecord() の結果へキーが含まれない。
    /// </summary>
    [Fact]
    public void ToKintoneRecordWhenSubTableIsUploadFalseOmitsFieldKey() {
        var model = new DummyModel {
            SubTableNotUploadable = [new DummySubTableItem { Text = "dummy" }]
        };

        var record = model.ToKintoneRecord();

        Assert.False(record.ContainsKey("SubTableNotUploadable"));
    }

    /// <summary>
    /// サブテーブルが IsUpload = true（デフォルト）の場合、ToKintoneRecord() の結果へキーが含まれる（既存挙動を維持）。
    /// </summary>
    [Fact]
    public void ToKintoneRecordWhenSubTableIsUploadTrueIncludesFieldKey() {
        var model = new DummyModel {
            SubTableUploadable = [new DummySubTableItem { Text = "dummy" }]
        };

        var json = SerializeField(model, "SubTableUploadable");

        Assert.Equal("{\"value\":[{\"id\":\"\",\"value\":{\"Text\":{\"value\":\"dummy\"}}}]}", json);
    }
    #endregion
}
