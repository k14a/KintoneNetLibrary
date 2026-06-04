using KintoneNetLibrary.Domain.Interfaces;

namespace KintoneNetLibrary.Domain.Common;

/// <summary>
/// Kintoneモデルが利用するサービスの参照を保持する静的コンテキスト。
/// アプリケーション起動時に KintoneServiceLocator.Initialize() 経由で設定される。
/// </summary>
public static class KintoneModelContext {
    private static Func<IKintoneModelCrudService>? _resolver;

    /// <summary>
    /// CRUDサービスのインスタンスを返す
    /// </summary>
    /// <exception cref="InvalidOperationException">未初期化の場合にスローされます</exception>
    public static IKintoneModelCrudService CrudService =>
        _resolver?.Invoke() ?? throw new InvalidOperationException(
            "KintoneModelContext が初期化されていません。KintoneServiceLocator.Initialize() を呼び出してください。");

    /// <summary>
    /// サービスリゾルバーを設定する
    /// </summary>
    /// <param name="resolver">サービスを返すデリゲート</param>
    public static void Configure(Func<IKintoneModelCrudService> resolver) {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// サービスを直接設定する（テスト用途）
    /// </summary>
    /// <param name="service">CRUDサービスのインスタンス</param>
    public static void Configure(IKintoneModelCrudService service) {
        ArgumentNullException.ThrowIfNull(service);
        _resolver = () => service;
    }

    /// <summary>
    /// リセット（テスト後のクリーンアップ用）
    /// </summary>
    public static void Reset() {
        _resolver = null;
    }
}
