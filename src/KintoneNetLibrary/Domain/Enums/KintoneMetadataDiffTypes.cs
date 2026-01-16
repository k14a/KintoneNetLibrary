namespace KintoneNetLibrary.Domain.Enums;

/// <summary>
/// Diff 種別
/// </summary>
public enum KintoneMetadataDiffTypes {
    /// <summary>
    /// フィールドの追加
    /// </summary>
    Added,
    /// <summary>
    /// フィールドの削除
    /// </summary>
    Removed,
    /// <summary>
    /// フィールドの変更
    /// </summary>
    Changed,
    /// <summary>
    /// オプションの差分
    /// </summary>
    ChangedOptions,
}