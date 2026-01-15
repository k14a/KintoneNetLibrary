namespace KintoneNetLibrary.Backup.Enums;

public enum RestoreMode {
    FullReplace, // 全削除 → 全再作成
    Upsert,      // 更新 or 作成
    Merge        // 追加のみ
}
