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
    // 必要に応じてここに他の定数を追加してください
}
