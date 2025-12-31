using Microsoft.Extensions.DependencyInjection;

namespace KintoneNetLibrary.Infrastructure.Helpers;

public static class KintoneServiceLocator {
    private static IServiceProvider? _provider;

    public static void Initialize(IServiceProvider provider) {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }
    public static T Resolve<T>() where T : notnull {
        if (_provider == null) {
            throw new InvalidOperationException("KintoneServiceLocator is not initialized.");
        }
        return _provider.GetRequiredService<T>();
    }
    public static void Reset() {
        _provider = null;
    }
}

