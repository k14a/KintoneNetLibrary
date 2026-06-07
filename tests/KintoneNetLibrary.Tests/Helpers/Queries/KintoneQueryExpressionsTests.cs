using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

/// <summary>
/// KintoneQueryの式ツリーからクエリ文字列への変換をテストするクラスです。
/// </summary>
public class KintoneQueryExpressionsTests {
    /// <summary>
    /// 単純な等価式の式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void ToQueryStringSimpleEqualsExpressionReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Title == "C#入門");
        var queryString = query.Build();

        Assert.Equal("Title = \"C#入門\"", queryString);
    }

    /// <summary>
    /// 単純な不等式の式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void ToQueryStringGreaterThanExpressionReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Price > 1000);
        var queryString = query.Build();

        Assert.Equal("Price > 1000", queryString);
    }

    /// <summary>
    /// 複数の条件をANDで組み合わせた式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void ToQueryStringAndExpressionReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Price > 1000 && b.Classification == "技術書");
        var queryString = query.Build();

        Assert.Equal("Price > 1000 and Classification = \"技術書\"", queryString);
    }

    /// <summary>
    /// 複数の条件をORで組み合わせた式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void ToQueryStringOrExpressionReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Classification == "技術書" || b.Classification == "SF");
        var queryString = query.Build();

        Assert.Equal("Classification = \"技術書\" or Classification = \"SF\"", queryString);
    }

    /// <summary>
    /// DateTime型の等価式の式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void ToQueryStringDateTimeEqualsReturnsCorrectQuery() {
        var targetDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new KintoneQuery<BookModel>().Where(b => b.ReleaseDate!.Value == targetDate);
        var queryString = query.Build();

        Assert.Equal("ReleaseDate = \"2024-01-01T00:00:00Z\"", queryString);
    }

    /// <summary>
    /// TimeOnly型の等価式の式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void ToQueryStringTimeOnlyEqualsReturnsCorrectQuery() {
        var time = new TimeOnly(9, 30);
        var query = new KintoneQuery<BookModel>().Where(b => b.TimeField.Value == time);
        var queryString = query.Build();

        Assert.Equal("TimeField = \"09:30\"", queryString);
    }

    /// <summary>
    /// 文字列の部分一致を表すlike式の式ツリーが、サポートされていないことを示す例外をスローすることをテストします。
    /// </summary>
    [Fact]
    public void ToQueryStringBooleanLikeExpressionThrowsNotSupported() {
        var exception = Assert.Throws<NotSupportedException>(() =>
            new KintoneQuery<BookModel>().Where(b => b.Title.Contains("test"))
        );
        Assert.Contains("like", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Idフィールドに対するin式の式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void WhereSimpleEqualityConditionReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(x => x.Title == "C#");
        var result = query.Build();
        Assert.Equal("Title = \"C#\"", result);
    }

    /// <summary>
    /// 複数の条件をANDで組み合わせた式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void WhereComplexAndOrConditionReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(x => x.Title == "C#" && x.Price > 1000);
        var result = query.Build();
        Assert.Equal("Title = \"C#\" and Price > 1000", result);
    }

    /// <summary>
    /// OrderByとThenByDescendingを組み合わせた式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void OrderByThenByDescendingWorksCorrectly() {
        var query = new KintoneQuery<BookModel>()
            .OrderBy(x => x.Title)
            .ThenByDescending(x => x.Price);
        var result = query.Build();
        Assert.Contains("order by Title asc, Price desc", result);
    }

    /// <summary>
    /// WhereIdInメソッドを使用してIdフィールドに対するin式の式ツリーが正しいクエリ文字列に変換されることをテストします。
    /// </summary>
    [Fact]
    public void WhereIdInAddsCorrectInClause() {
        var query = new KintoneQuery<BookModel>().WhereIdIn(["123", "456"]);
        var result = query.Build();
        Assert.Equal("$id in (\"123\", \"456\")", result);
    }

    /// <summary>
    /// SetQueryメソッドを使用してクエリ文字列を直接設定した場合、Buildメソッドがそのクエリ文字列を返すことをテストします。
    /// </summary>
    [Fact]
    public void BuildWithOffsetThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<KintoneException>(() => query.SetQuery("offset 10").Build());
        Assert.Contains("offset", ex.Message);
    }

    /// <summary>
    /// フィールド名に "offset" を含む場合でも、Buildメソッドが例外をスローせず、正しいクエリ文字列を生成することをテストします。
    /// </summary>
    [Fact]
    public void BuildFieldNameContainsOffsetDoesNotThrowException() {
        // Arrange
        var query = new KintoneQuery<BookModel>();
        // 「OffsetIncludedField」は、"offset" を含むフィールド名として想定
        query.SetQuery("OffsetIncludedField = \"test\"");
        // Act & Assert: 例外はスローされないはず
        var result = query.Build();
        Assert.Equal("OffsetIncludedField = \"test\"", result);
    }

    /// <summary>
    /// 式ツリーを使用して、タイトルが特定の文字列に等しい条件を表すクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryTitleEqualsString() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "C#入門")
            .Build();
        Assert.Equal("Title = \"C#入門\"", query);
    }
    /// <summary>
    /// 式ツリーを使用して、価格が特定の値より大きい条件を表すクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryPriceGreaterThan2000() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price > 2000)
            .Build();
        Assert.Equal("Price > 2000", query);
    }

    /// <summary>
    /// 式ツリーを使用して、分類が特定の文字列に等しい条件を表すクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryClassificationEqualsSF() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Classification == "SF")
            .Build();
        Assert.Equal("Classification = \"SF\"", query);
    }

    /// <summary>
    /// 式ツリーを使用して、リリース日が特定の日付より前である条件を表すクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryReleaseDateBefore20250101() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.ReleaseDate! < new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        Assert.Equal("ReleaseDate < \"2025-01-01T00:00:00Z\"", query);
    }

    /// <summary>
    /// 式ツリーを使用して、リリース日が特定の日付より前である条件を表すクエリが、ローカル時間を考慮して正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryReleaseDateBefore20250101LocalTime() {
        var localDateTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var query = new KintoneQuery<BookModel> {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")
        }
        .Where(x => x.ReleaseDate < localDateTime)
        .Build();
        // 東京標準時 = UTC+9 → UTCでは2024-12-31T15:00:00Zになる
        Assert.Equal("ReleaseDate < \"2024-12-31T15:00:00Z\"", query);
    }

    /// <summary>
    /// 式ツリーを使用して、マルチセレクトフィールドのいずれかが特定の値に等しい条件を表すクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryMultiSelectorAny() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.MultiSelector.Any(s => new[] { "選択肢1", "選択肢2" }.Contains(s)))
            .Build();
        Assert.Equal("MultiSelector in (\"選択肢1\", \"選択肢2\")", query);
    }

    /// <summary>
    /// 式ツリーを使用して、複数の条件を組み合わせ、さらにOrderByとLimitを使用したクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryCombinedConditionsWithOrderLimit() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price >= 1000)
            .And(x => x.Recommendation == "5")
            .OrderByDescending(x => x.DateField)
            .Limit(20)
            .Build();
        Assert.Equal("Price >= 1000 and Recommendation = \"5\" order by DateField desc limit 20", query);
    }

    /// <summary>
    /// 式ツリーを使用して、複数の条件を組み合わせ、さらにOrderByとThenByDescending、Limitを使用したクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryOrderByThenByDescendingLimit() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "村上春樹全集")
            .OrderBy(x => x.ReleaseDate)
            .ThenByDescending(x => x.Price)
            .Limit(10)
            .Build();
        Assert.Equal("Title = \"村上春樹全集\" order by ReleaseDate asc, Price desc limit 10", query);
    }

    /// <summary>
    /// 式ツリーを使用して、複数の条件を組み合わせ、さらにOrderByとThenByを使用したクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryOrderByMultipleFieldsAscending() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price > 1500)
            .OrderBy(x => x.ReleaseDate)
            .ThenBy(x => x.Title)
            .Build();
        Assert.Equal("Price > 1500 order by ReleaseDate asc, Title asc", query);
    }

    /// <summary>
    /// 式ツリーを使用して、複数の条件を組み合わせ、さらにOrderByDescendingとThenByを使用したクエリが正しく生成されることをテストします。j
    /// </summary>
    [Fact]
    public void QueryOrderByDescendingThenBy() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price <= 2000)
            .OrderByDescending(x => x.Price)
            .ThenBy(x => x.Title)
            .Build();
        Assert.Equal("Price <= 2000 order by Price desc, Title asc", query);
    }

    /// <summary>
    /// 式ツリーを使用して、複数の条件を組み合わせ、さらにOrderByDescendingとThenByDescendingを使用したクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryOrderByDescendingThenByDescending() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "物語シリーズ")
            .OrderByDescending(x => x.ReleaseDate)
            .ThenByDescending(x => x.Price)
            .Build();
        Assert.Equal("Title = \"物語シリーズ\" order by ReleaseDate desc, Price desc", query);
    }

    /// <summary>
    /// 式ツリーを使用して、OrderByとLimitのみを組み合わせたクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryOrderByWithLimitOnly() {
        var query = new KintoneQuery<BookModel>()
            .OrderBy(x => x.ReleaseDate)
            .Limit(5)
            .Build();
        Assert.Equal("order by ReleaseDate asc limit 5", query);
    }

    /// <summary>
    /// 式ツリーを使用して、Idフィールドが特定の値に等しい条件を表すクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryWhereIdEqualsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .WhereIdEquals("abc123")
            .Build();

        Assert.Equal("$id=\"abc123\"", query);
    }

    /// <summary>
    /// 式ツリーを使用して、Idフィールドが複数の値のいずれかに等しい条件を表すクエリが正しく生成されることをテストします。
    /// </summary>
    [Fact]
    public void QueryWhereIdsEqualsMultipleIds() {
        var query = new KintoneQuery<BookModel>()
            .WhereIdsEquals(new[] { "a", "b", "c" })
            .Build();

        Assert.Equal("$id=\"a\" or $id=\"b\" or $id=\"c\"", query);
    }

    /// <summary>
    /// 式ツリーを使用して、クエリ文字列を直接設定した場合、Buildメソッドがそのクエリ文字列を返すことをテストします。
    /// </summary>
    [Fact]
    public void QuerySetQueryOverridesConditions() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price > 1000)
            .SetQuery("CustomField = \"abc\"")
            .Build();

        Assert.Equal("CustomField = \"abc\"", query);
    }

    /// <summary>
    /// 式ツリーを使用して、クエリが空の場合、Buildメソッドが空文字列を返すことをテストします。
    /// </summary>
    [Fact]
    public void QueryEmptyBuildReturnsEmptyString() {
        var query = new KintoneQuery<BookModel>().Build();
        Assert.Equal(string.Empty, query);
    }

    /// <summary>
    /// 式ツリーを使用して、クエリが複雑な条件を含む場合でも、ToStringメソッドがBuildメソッドと同じクエリ文字列を返すことをテストします。
    /// </summary>
    [Fact]
    public void ToStringReturnsSameAsBuild() {
        var query = new KintoneQuery<BookModel>()
            .Where(b => b.Title == "C#入門")
            .OrderBy(b => b.Id)
            .Limit(10);

        Assert.Equal(query.Build(), query.ToString());
    }

}
