using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Tests.Models;

public class BookModel : KintoneModelBase {
    public int AppID => TestEnv.Settings.AppID;
    /// <summary>
    /// タイトル
    /// </summary>
    [KintoneItem(fieldCode: "Title", KintoneFieldType.SingleLineText)]
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// 価格
    /// </summary>
    [KintoneItem(fieldCode: "Price", KintoneFieldType.Number)]
    public int? Price { get; set; }
    /// <summary>
    /// UUID
    /// </summary>
    [KintoneItem(fieldCode: "UUID", isKey: true, fieldType: KintoneFieldType.SingleLineText)]
    public string Uuid { get; set; } = string.Empty;
    /// <summary>
    /// 分野
    /// </summary>
    /// <value>雑誌<br/>技術書<br/>SF</value>
    [KintoneItem(fieldCode: "Classification", KintoneFieldType.DropDown)]
    public string Classification { get; set; } = string.Empty;
    /// <summary>
    /// 発売日
    /// </summary>
    [KintoneItem(fieldCode: "ReleaseDate", fieldType: KintoneFieldType.DateTime)]
    public KintoneDateTime? ReleaseDate { get; set; } = null;
    /// <summary>
    /// レビュー
    /// </summary>
    [KintoneItem(fieldCode: "Reviews", KintoneFieldType.MultiLineText)]
    public string Reviews { get; set; } = string.Empty;
    /// <summary>
    /// レイティング
    /// </summary>
    [KintoneItem(fieldCode: "Rating", fieldType: KintoneFieldType.Number)]
    public decimal Rating { get; set; }
    /// <summary>
    /// おすすめ度
    /// </summary>
    /// <value>5：強く勧めたい<br/>4：おすすめ<br/>3：どちらでもない<br/>2：おすすめしない<br/>1：まったく勧めない</value>
    [KintoneItem(fieldCode: "Recommendation", KintoneFieldType.RadioButton)]
    public string Recommendation { get; set; } = string.Empty;
    /// <summary>
    /// チェックボックス
    /// </summary>
    /// <value>チェック1<br/>チェック2<br/>チェック3<br/>チェック4</value>
    [KintoneItem(fieldCode: "Checkboxes", KintoneFieldType.CheckBox)]
    public IEnumerable<string> CheckBoxes { get; set; } = [];
    /// <summary>
    /// Webアドレス
    /// </summary>
    [KintoneItem(fieldCode: "WebAddress", fieldType: KintoneFieldType.LinkUrl)]
    public string WebAddress { get; set; } = string.Empty;
    /// <summary>
    /// 電話番号
    /// </summary>
    [KintoneItem(fieldCode: "Telephone", fieldType: KintoneFieldType.LinkTelephone)]
    public string Telephone { get; set; } = string.Empty;
    /// <summary>
    /// メールアドレス
    /// </summary>
    [KintoneItem(fieldCode: "Email", KintoneFieldType.LinkEmail)]
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// 日付
    /// </summary>
    [KintoneItem(fieldCode: "DateField", KintoneFieldType.Date)]
    public KintoneDateTime DateField { get; set; } = new();
    /// <summary>
    /// 時刻
    /// </summary>
    [KintoneItem(fieldCode: "TimeField", KintoneFieldType.Time)]
    public KintoneTimeOnly TimeField { get; set; } = new();
    /// <summary>
    /// 複数選択
    /// </summary>
    /// <value>選択肢1<br/>選択肢2<br/>選択肢3<br/>選択肢4<br/>選択肢5</value>
    [KintoneItem(fieldCode: "MultiSelector", KintoneFieldType.MultiSelect)]
    public IEnumerable<string> MultiSelector { get; set; } = [];
    /// <summary>
    /// 添付ファイル
    /// </summary>
    [KintoneItem(fieldCode: "Files", KintoneFieldType.File)]
    public IList<KintoneFile> Files { get; set; } = [];
    /// <summary>
    /// サブテーブル
    /// </summary>
    [KintoneItem("Details", KintoneFieldType.SubTable)]
    public List<BookModelDetail>? Details { get; set; }
    /// <summary>
    /// リッチテキスト
    /// </summary>
    [KintoneItem(fieldCode: "RichText", fieldType: KintoneFieldType.RichText)]
    public string RichText { get; set; } = string.Empty;
    /// <summary>
    /// ユーザー選択
    /// </summary>
    [KintoneItem(fieldCode: "UserSelect", fieldType: KintoneFieldType.UserSelect)]
    public IEnumerable<KintoneUser> UserSelect { get; set; } = [];
    /// <summary>
    /// グループ選択
    /// </summary>
    [KintoneItem(fieldCode: "GroupSelect", KintoneFieldType.GroupSelect)]
    public IEnumerable<KintoneUser> GroupSelect { get; set; } = [];
    /// <summary>
    /// 組織選択
    /// </summary>
    [KintoneItem(fieldCode: "DivisionSelect", KintoneFieldType.OrganizationSelect)]
    public IEnumerable<KintoneUser> DivisionSelect { get; set; } = [];
}
public class BookModelDetail : KintoneSubTableBase {
    /// <summary>
    /// No
    /// </summary>
    [KintoneItem(fieldCode: "No", KintoneFieldType.Number)]
    public int No { get; set; }
    /// <summary>
    /// 取扱店
    /// </summary>
    [KintoneItem(fieldCode: "StoreName", KintoneFieldType.SingleLineText)]
    public string StoreName { get; set; } = string.Empty;
    /// <summary>
    /// 配送日
    /// </summary>
    [KintoneItem(fieldCode: "DeliveryDate", KintoneFieldType.Date)]
    public KintoneDateTime? DeliveryDate { get; set; }
    /// <summary>
    /// 梱包状態
    /// </summary>
    /// <value>梱包する</value>
    [KintoneItem(fieldCode: "PackageType", KintoneFieldType.CheckBox)]
    public IEnumerable<string> PackageType { get; set; } = [];
}
