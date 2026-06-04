using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.Infrastructure.Helpers;

/// <summary>
/// Kintoneサービスロケーター
/// </summary>
public static class KintoneServiceLocator {
    private static IServiceProvider? _provider;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="provider">サービスプロバイダー</param>
    /// <exception cref="ArgumentNullException">provider が null の場合にスローされます</exception>
    public static void Initialize(IServiceProvider provider) {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        KintoneModelContext.Configure(() => _provider.GetRequiredService<IKintoneModelCrudService>());
    }
    /// <summary>
    /// サービスの解決
    /// </summary>
    /// <typeparam name="T">解決対象のサービスの型</typeparam>
    /// <returns>解決されたサービスのインスタンス</returns>
    /// <exception cref="InvalidOperationException">サービスロケーターが初期化されていない場合にスローされます</exception>
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
        KintoneModelContext.Reset();
    }
}

