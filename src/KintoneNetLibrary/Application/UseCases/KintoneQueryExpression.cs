using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;

namespace KintoneNetLibrary.Application.UseCases;

/// <summary>
/// KintoneQueryExpression クラスは、Kintone のクエリ式を表現します。
/// </summary>
/// <typeparam name="T">KintoneModelBase&lt;T&gt; を継承したモデルクラス</typeparam>
/// <remarks>
/// このクラスは、Kintone のクエリ式を構築するためのメソッドを提供します。
/// クエリ式は、フィールドの値の比較、ソート条件の追加、フィールドの null チェックなどを行うために使用されます。
/// また、クエリ式を文字列に変換するためのメソッドも提供しています。
/// </remarks>
/// <paramref name="expression">クエリ式を表現する Expression</paramref>
/// <exception cref="ArgumentNullException">expression が null の場合にスローされます。</exception>
/// <exception cref="ArgumentException">expression が有効なクエリ式を表現していない場合にスローされます。</exception>
public class KintoneQueryExpression<T>(Expression<Func<T, bool>> expression) {
    /// <summary>
    /// クエリ式を表現すためのフィールドです。
    /// </summary>
    public Expression<Func<T, bool>> Predicate { get; set; } = expression ?? throw new ArgumentNullException(nameof(expression));
    /// <summary>
    /// タイムゾーン情報を表現するプロパティです。
    /// </summary>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>
    /// クエリ式を文字列に変換します。
    /// </summary>
    /// <remarks>
    /// このメソッドは、クエリ式を Kintone のクエリ文字列に変換します。
    /// クエリ文字列は、Kintone の API で使用される形式に従います。
    /// </remarks>
    /// <returns>クエリ式を表現する文字列</returns>
    public string ToQueryString() {
        if (this.Predicate == null) { return string.Empty; }
        var visitor = new KintoneExpressionVisitor { TimeZone = this.TimeZone };
        return visitor.ToQueryString(this.Predicate.Body);
    }
}
