namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintoneアプリのメタデータを表します。
/// </summary>
public class KintoneAppMetadata {
    /// <summary>
    /// アプリIDを取得または設定します。
    /// </summary>
    public int AppId { get; set; }

    /// <summary>
    /// リビジョン番号を取得または設定します。
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// フィールドのメタデータの一覧を取得または設定します。
    /// </summary>
    public IReadOnlyList<KintoneFieldMetadata> Fields { get; set; } = default!;

    /// <summary>
    /// レイアウトのメタデータを取得または設定します。
    /// </summary>
    public KintoneLayoutMetadata Layout { get; set; } = default!;
}
