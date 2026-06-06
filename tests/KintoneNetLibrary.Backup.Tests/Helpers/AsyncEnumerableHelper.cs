namespace KintoneNetLibrary.Backup.Tests.Helpers;

internal static class AsyncEnumerableHelper {
    internal static async IAsyncEnumerable<T> Create<T>(IEnumerable<T> items) {
        foreach (var item in items) {
            yield return item;
            await Task.Yield();
        }
    }

    internal static IAsyncEnumerable<T> Empty<T>() => Create<T>([]);
}
