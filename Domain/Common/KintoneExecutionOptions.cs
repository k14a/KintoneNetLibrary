namespace KintoneNetLibrary.Domain.Common;

/// <summary>
/// KintoneExecutionOptionsクラスは、Kintone APIの実行オプションを定義します。
/// このクラスは、Kintone APIの実行時に使用されるオプションを提供します。
/// これにより、Kintone APIの実行方法をカスタマイズでき、特定の要件に応じて動作を調整できます。
/// </summary>
/// <remarks>
/// このクラスは、Kintone APIの実行時に使用されるオプションを一元管理するために使用されます。
/// これにより、Kintone APIの実行方法をカスタマイズでき、特定の要件に応じて動作を調整できます。
/// 例えば、最大同時実行数やエラー時の再試行オプション、作成から更新への再試行オプションなどが含まれています。
/// また、これらのオプションは、Kintone APIの実行時に適用され、APIの動作を制御します。
/// </remarks>
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
