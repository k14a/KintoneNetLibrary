using System.Collections.Generic;

namespace KintoneNetLibrary.Extensions;

/// <summary>
/// IList&lt;T&gt; 用のユーティリティ拡張メソッド。
/// </summary>
public static class IListExtensions
{
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
}
