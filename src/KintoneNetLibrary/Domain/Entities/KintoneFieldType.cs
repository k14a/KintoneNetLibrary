namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone レコードの基本タイプ定義
/// </summary>
public enum KintoneFieldType {
    /// <summary>
    /// 不明
    /// </summary>
    Unknown,
    /// <summary>
    /// 一行テキストフィールド
    /// </summary>
    SingleLineText,
    /// <summary>
    /// 数値フィールド
    /// </summary>
    Number,
    /// <summary>
    /// 計算フィールド
    /// </summary>
    Calc,
    /// <summary>
    /// 複数行テキストフィールド
    /// </summary>
    MultiLineText,
    /// <summary>
    /// リッチエディターフィールド
    /// </summary>
    RichText,
    /// <summary>
    /// リンクフィールド（非推奨）
    /// </summary>
    [Obsolete("リンクタイプを明確に指定してください。LinkUrl, LinkTelephone, LinkEmail を使用してください。")]
    Link,
    /// <summary>
    /// URLリンクフィールド
    /// </summary>
    LinkUrl,
    /// <summary>
    /// 電話リンクフィールド
    /// </summary>
    LinkTelephone,
    /// <summary>
    /// メールリンクフィールド
    /// </summary>
    LinkEmail,
    /// <summary>
    /// チェックボックスフィールド
    /// </summary>
    CheckBox,
    /// <summary>
    /// ラジオボタンフィールド
    /// </summary>
    RadioButton,
    /// <summary>
    /// ドロップダウンフィールド
    /// </summary>
    DropDown,
    /// <summary>
    /// 複数選択フィールド
    /// </summary>
    MultiSelect,
    /// <summary>
    /// 添付ファイルフィールド
    /// </summary>
    File,
    /// <summary>
    /// 日付フィールド
    /// </summary>
    Date,
    /// <summary>
    /// 時間フィールド
    /// </summary>
    Time,
    /// <summary>
    /// 日時フィールド
    /// </summary>
    DateTime,
    /// <summary>
    /// ユーザー選択フィールド
    /// </summary>
    UserSelect,
    /// <summary>
    /// 組織選択フィールド
    /// </summary>
    OrganizationSelect,
    /// <summary>
    /// グループ選択フィールド
    /// </summary>
    GroupSelect,
    /// <summary>
    /// 作成日時フィールド
    /// </summary>
    CreatedTime,
    /// <summary>
    /// 更新日時フィールド
    /// </summary>
    UpdatedTime,
    /// <summary>
    /// 作成者フィールド
    /// </summary>
    CreatedBy,
    /// <summary>
    /// 更新者フィールド
    /// </summary>
    UpdatedBy,
    /// <summary>
    /// レコード番号フィールド
    /// </summary>
    RecordNumber,
    /// <summary>
    /// ステータスフィールド
    /// </summary>
    Status,
    /// <summary>
    /// 担当者フィールド
    /// </summary>
    Assignee,
    /// <summary>
    /// カテゴリーフィールド
    /// </summary>
    Category,
    /// <summary>
    /// サブテーブルフィールド
    /// </summary>
    SubTable,
    /// <summary>
    /// ルックアップフィールド
    /// </summary>
    Lookup,
    /// <summary>
    /// ステータス担当者フィールド
    /// </summary>
    StatusAssignee,
    /// <summary>
    /// リビジョンフィールド
    /// </summary>
    Revision,
    /// <summary>
    /// 作成者フィールド
    /// </summary>
    Creator,
    /// <summary>
    /// 更新者フィールド
    /// </summary>
    Modifier,
}
