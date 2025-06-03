using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Tests.Models;

public class BookModel : KintoneModelBase {
    public override int AppID => TestEnv.Settings.AppID;

    /// <summary>
    /// タイトル
    /// </summary>
    [KintoneItem(fieldCode: "Title")]
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// 価格
    /// </summary>
    [KintoneItem(fieldCode: "Price")]
    public int? Price { get; set; }
    /// <summary>
    /// UUID
    /// </summary>
    [KintoneItem(fieldCode: "UUID", isKey: true)]
    public string Uuid { get; set; } = string.Empty;
    /// <summary>
    /// 分野
    /// </summary>
    /// <value>雑誌<br/>技術書<br/>SF</value>
    [KintoneItem(fieldCode: "Classification")]
    public string Classification { get; set; } = string.Empty;
    /// <summary>
    /// 発売日
    /// </summary>
    [KintoneItem(fieldCode: "ReleaseDate", dateType: KintoneDateTimeType.DateTime)]
    public DateTime? ReleaseDate { get; set; }
    /// <summary>
    /// レビュー
    /// </summary>
    [KintoneItem(fieldCode: "Reviews")]
    public string Reviews { get; set; } = string.Empty;
    /// <summary>
    /// おすすめ度
    /// </summary>
    /// <value>5：強く勧めたい<br/>4：おすすめ<br/>3：どちらでもない<br/>2：おすすめしない<br/>1：まったく勧めない</value>
    [KintoneItem(fieldCode: "Recommendation")]
    public string Recommendation { get; set; } = string.Empty;
    /// <summary>
    /// チェックボックス
    /// </summary>
    /// <value>チェック1<br/>チェック2<br/>チェック3<br/>チェック4</value>
    [KintoneItem(fieldCode: "Checkboxes")]
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
    [KintoneItem(fieldCode: "Email")]
    public string Email { get; set; } = string.Empty;
}
