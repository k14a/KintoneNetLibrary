namespace KintoneNetLibrary.Domain.Common;

public static class KintoneConstants {
    /// <summary>
    /// Kintone APIが一度に処理可能な最大レコード数（例：100件）
    /// </summary>
    public const int KintoneLimit = 100;
    /// <summary>
    /// KintoneカーソルAPIで一度に取得可能な最大件数（例：500件）
    /// </summary>
    public const int CursorFetchLimit = 500;
    /// <summary>
    /// Kintoneデータ削除上限
    /// </summary>
    public const int KintoneDeleteLimit = 100;
    /// <summary>
    /// Kintoneのフィールドコード最大文字数
    /// </summary>
    public const int KintoneFieldCodeMaxLength = 128;
    /// <summary>
    /// Kintoneでのアップロード可能ファイルサイズ(100MB)
    /// </summary>
    public const long MaxUploadFileSize = 100 * 1024 * 1024;
    /// <summary>
    /// Kintoneでの1APIリクエストでの最大ファイルサイズ(20)
    /// </summary>
    public const int MaxUploadFileCount = 20;
    // 必要に応じてここに他の定数を追加してください
}
