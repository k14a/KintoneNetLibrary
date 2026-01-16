namespace KintoneNetLibrary.Backup.Domain.Enums;

/// <summary>
/// リストアモード
/// </summary>
public enum RestoreMode {
    /// <summary>
    /// 完全置換
    /// </summary>
    /// <remarks>
    /// 既存レコードを全て削除し、バックアップデータのみを復元します
    /// </remarks>
    FullReplace,
    /// <summary>
    /// 更新 or 作成
    /// </summary>
    /// <remarks>
    /// バックアップデータのレコードIDが存在する場合は更新、存在しない場合は作成します
    /// </remarks>
    Upsert,
    /// <summary>
    /// 追加のみ
    /// </summary>
    /// <remarks>
    /// 既存レコードを変更せず、バックアップデータのレコードIDが存在しない場合のみ作成します
    /// </remarks>
    Merge,
}
