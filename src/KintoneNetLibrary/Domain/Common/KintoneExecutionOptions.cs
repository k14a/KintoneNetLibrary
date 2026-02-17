namespace KintoneNetLibrary.Domain.Common;

/// <summary>
/// KintoneExecutionOptionsクラスは、KintoneNetLibraryで使用されるKintone APIの実行オプションを定義するクラスです。
/// このクラスには、Kintone APIの実行に関連するオプションが含まれており、最大同時実行数やエラー時の再試行設定などが定義されています。
/// </summary>
public class KintoneExecutionOptions {
    /// <summary>
    /// 最大同時実行数を取得または設定します。
    /// このプロパティは、Kintone APIの並列登録処理における最大同時実行数を指定します。
    /// </summary>
    public int MaxConcurrency { get; set; } = KintoneConstants.MaxConcurrentRequestCount;

    /// <summary>
    /// エラー時の再試行を有効にするかどうかを取得または設定します。
    /// このプロパティは、Kintone APIの実行中にエラーが発生した場合に再試行を行うかどうかを指定します。
    /// </summary>
    public bool EnableSingleRetryOnError { get; set; } = true;

    /// <summary>
    /// 作成から更新への再試行を有効にするかどうかを取得または設定します。
    /// このプロパティは、Kintone APIの実行中にレコードの作成から更新への再試行を行うかどうかを指定します。
    /// </summary>
    public bool EnableCreateToUpdateRetry { get; set; } = true;
}
