namespace KintoneNetLibrary.Extensions;

/// <summary>
/// IList&lt;T&gt; に対する拡張メソッドを提供するクラス
/// </summary>
public static class IListExtensions {
    /// <summary>
    /// IList&lt;T&gt;.AddRangeをIList&lt;T&gt;に追加
    /// </summary>
    /// <typeparam name="T">要素の型</typeparam>
    /// <param name="target">追加先のIList&lt;T&gt;</param>
    /// <param name="source">追加する要素のコレクション</param>
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
    /// <typeparam name="T">要素の型</typeparam>
    /// <param name="list">操作対象のIList&lt;T&gt;</param>
    /// <param name="match">削除条件を示すPredicate&lt;T&gt;</param>
    /// <returns>削除された要素の数</returns>
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
