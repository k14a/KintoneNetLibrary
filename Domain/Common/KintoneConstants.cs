namespace KintoneNetLibrary.Domain.Common;

/// <summary>
/// KintoneConstantsクラスは、Kintoneに関連する定数を定義します。
/// このクラスは、Kintone APIの制限や設定に関する定数を提供します。
/// これにより、Kintone APIの使用時に一貫性を保ち、コードの可読性を向上させます。
/// </summary>
/// <remarks>
/// このクラスは、Kintone APIの制限や設定に関する定数を一元管理するために使用されます。
/// これにより、Kintone APIの使用時に一貫性を保ち、コードの可読性を向上させます。
/// 例えば、Kintone APIが一度に処理可能な最大レコード数や、カーソルAPIで一度に取得可能な最大件数などの定数が含まれています。
/// また、Kintoneのフィールドコードの最大文字数や、アップロード可能ファイルサイズ、最大アップロードファイル数などの制限も定義されています。
/// </remarks>
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
    /// <summary>
    /// Kintoneの並列登録処理における最大同時実行数（例：4）
    /// </summary>
    public const int MaxConcurrentRequestCount = 4;
    // 必要に応じてここに他の定数を追加してください
}
