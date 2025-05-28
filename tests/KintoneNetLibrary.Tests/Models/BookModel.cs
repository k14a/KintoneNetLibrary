using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Tests.Models;

// Kintone アプリ上に「タイトル(string)」「価格(int)」フィールドがある前提
public class BookModel : KintoneModelBase {
    public override int AppID => TestEnv.Settings.AppID;

    [KintoneItem(fieldCode: "Title", isUpload: true)]
    public string Title { get; set; } = string.Empty;
    [KintoneItem(fieldCode: "Price", isUpload: true)]
    public int Price { get; set; }
    [KintoneItem(fieldCode: "UUID", isUpload: true, isKey: true)]
    public string Uuid { get; set; } = string.Empty;
}