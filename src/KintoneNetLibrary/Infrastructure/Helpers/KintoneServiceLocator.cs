using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.Infrastructure.Helpers;

// コメントは日本語で記述
/// <summary>
/// Kintoneサービスロケーター
/// </summary>
public static class KintoneServiceLocator {
    private static IServiceProvider? _provider;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="provider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Initialize(IServiceProvider provider) {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }
    /// <summary>
    /// サービスの解決
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static T Resolve<T>() where T : notnull {
        if (_provider == null) {
            throw new InvalidOperationException("KintoneServiceLocator is not initialized.");
        }
        return _provider.GetRequiredService<T>();
    }
    /// <summary>
    /// リセット
    /// </summary>
    public static void Reset() {
        _provider = null;
    }
}

