namespace KintoneNetLibrary.Infrastructure.Internal;

/// <summary>
/// Kintone API エンドポイントの定数を定義するクラス
/// </summary>
internal static class KintoneApiEndpoints {
    /// <summary>
    /// レコード操作に関するエンドポイント
    /// </summary>
    public const string GetSingleRecord = "record.json";
    /// <summary>
    /// 複数レコード操作に関するエンドポイント
    /// </summary>
    public const string GetRecords = "records.json";
    /// <summary>
    /// レコードの追加、更新、削除に関するエンドポイント
    /// </summary>
    public const string AddRecord = "record.json";
    /// <summary>
    /// 複数レコードの追加、更新、削除に関するエンドポイント
    /// </summary>
    public const string AddRecords = "records.json";
    /// <summary>
    /// レコードの更新に関するエンドポイント
    /// </summary>
    public const string UpdateRecord = "record.json";
    /// <summary>
    /// 複数レコードの更新に関するエンドポイント
    /// </summary>
    public const string UpdateRecords = "records.json";
    /// <summary>
    /// レコードの削除に関するエンドポイント
    /// </summary>
    public const string DeleteRecord = "record.json";
    /// <summary>
    /// 複数レコードの削除に関するエンドポイント
    /// </summary>
    public const string DeleteRecords = "records.json";
    /// <summary>
    /// カーソル操作に関するエンドポイント
    /// </summary>
    public const string Cursor = "records/cursor.json";
    /// <summary>
    /// アプリのフィールド情報取得に関するエンドポイント
    /// </summary>
    public const string GetAppFields = "app/form/fields.json";
    /// <summary>
    /// アプリのレイアウト情報取得に関するエンドポイント
    /// </summary>
    public const string GetAppLayout = "app/form/layout.json";
}
