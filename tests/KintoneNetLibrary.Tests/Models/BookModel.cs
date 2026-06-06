using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Tests.Models;

/// <summary>
/// クエリビルダーなどのユニットテスト用モデル。実環境への接続は行わない。
/// </summary>
public class BookModel : KintoneModelBase<BookModel> {
    public override int AppID { get; init; } = 0;
    public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummy", "dummy");

    [KintoneItem(fieldCode: "Title", KintoneFieldType.SingleLineText)]
    public string Title { get; set; } = string.Empty;

    [KintoneItem(fieldCode: "Price", KintoneFieldType.Number)]
    public int? Price { get; set; }

    [KintoneItem(fieldCode: "Classification", KintoneFieldType.DropDown)]
    public string Classification { get; set; } = string.Empty;

    [KintoneItem(fieldCode: "ReleaseDate", fieldType: KintoneFieldType.DateTime)]
    public KintoneDateTime? ReleaseDate { get; set; }

    [KintoneItem(fieldCode: "TimeField", KintoneFieldType.Time)]
    public KintoneTimeOnly TimeField { get; set; } = new();

    [KintoneItem(fieldCode: "MultiSelector", KintoneFieldType.MultiSelect)]
    public IEnumerable<string> MultiSelector { get; set; } = [];

    [KintoneItem(fieldCode: "Rating", fieldType: KintoneFieldType.Number)]
    public decimal Rating { get; set; }

    [KintoneItem(fieldCode: "Recommendation", KintoneFieldType.RadioButton)]
    public string Recommendation { get; set; } = string.Empty;

    [KintoneItem(fieldCode: "DateField", KintoneFieldType.Date)]
    public KintoneDateTime DateField { get; set; } = new();
}
