namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneアプリのレイアウトメタデータを表すクラス
/// </summary>
public class KintoneLayoutMetadata {
    /// <summary>
    /// リビジョン番号を取得または設定します。
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// レイアウトブロックの一覧を取得または設定します。
    /// </summary>
    public List<KintoneLayoutBlock> Layout { get; set; } = [];
}

/// <summary>
/// Kintoneのレイアウトブロックを表す抽象クラス
/// </summary>
public abstract class KintoneLayoutBlock {
    /// <summary>
    /// レイアウトブロックのタイプを取得または設定します。
    /// 例: "ROW", "GROUP", "FIELD" など
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Kintoneのレイアウト(行)を表すクラス
/// </summary>
public class KintoneLayoutRow : KintoneLayoutBlock {
    /// <summary>
    /// レイアウト行内のフィールドの一覧を取得または設定します。
    /// </summary>
    public List<KintoneLayoutField> Fields { get; set; } = [];
}

/// <summary>
/// Kintoneのレイアウト(サブテーブル)を表すクラス
/// </summary>
public class KintoneLayoutSubTable : KintoneLayoutBlock {
    /// <summary>
    /// サブテーブルのコードを取得または設定します。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// サブテーブル内のフィールドの一覧を取得または設定します。
    /// </summary>
    public List<KintoneLayoutField> Fields { get; set; } = [];
}

/// <summary>
/// Kintoneのレイアウト(グループ)を表すクラス
/// </summary>
public class KintoneLayoutGroup : KintoneLayoutBlock {
    /// <summary>
    /// グループのコードを取得または設定します。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// グループ内のフィールドの一覧を取得または設定します。
    /// </summary>
    public List<KintoneLayoutBlock> Layout { get; set; } = [];
}

/// <summary>
/// Kintoneのレイアウトフィールドを表すクラス
/// </summary>
public class KintoneLayoutField {
    /// <summary>
    /// フィールドコードを取得または設定します。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// フィールドコードを取得または設定します。
    /// LABEL/SPACER/HRなどのフィールド以外は、通常はコードが入ります。
    /// </summary>
    public string? Code { get; set; }
}
