using KintoneNetLibrary.Model;

namespace KintoneNetLibrary.Tests.Models;

// Kintone アプリ上に「タイトル(string)」「価格(int)」フィールドがある前提
public class BookModel : KintoneModelBase
{
    public override int AppID => TestEnv.Settings.AppID;

    public string Title { get; set; } = string.Empty;
    public int    Price { get; set; }
}