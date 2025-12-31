namespace KintoneNetLibrary.Extensions;

/// <summary>
/// IList&lt;T&gt; 用のユーティリティ拡張メソッド。
/// </summary>
public static class IListExtensions {
    /// <summary>
    /// IEnumerable&lt;T&gt; の要素を IList&lt;T&gt; に追加します。
    /// </summary>
    public static void AddRange<T>(this IList<T> target, IEnumerable<T> source) {
        ArgumentNullException.ThrowIfNull(target);

        if (source == null) {
            return; // 何も追加しない
        }

        foreach (var item in source) {
            target.Add(item);
        }
    }
    /// <summary>
    /// List&lt;T&gt;.RemoveAllをIList&lt;T&gt;に追加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="match"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static int RemoveAll<T>(this IList<T> list, Predicate<T> match) {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(match);

        var removedCount = 0;
        for (int i = list.Count - 1; i >= 0; i--) {
            if (match(list[i])) {
                list.RemoveAt(i);
                removedCount++;
            }
        }

        return removedCount;
    }
}
