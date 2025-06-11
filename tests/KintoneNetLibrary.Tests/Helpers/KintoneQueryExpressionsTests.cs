using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers;

public class KintoneQueryExpressionsTests {
    [Fact]
    public void ToQueryString_SimpleEqualsExpression_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Title == "C#入門");
        var queryString = query.Build();

        Assert.Equal("Title = \"C#入門\"", queryString);
    }
    [Fact]
    public void ToQueryString_GreaterThanExpression_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Price > 1000);
        var queryString = query.Build();

        Assert.Equal("Price > 1000", queryString);
    }
    [Fact]
    public void ToQueryString_AndExpression_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Price > 1000 && b.Classification == "技術書");
        var queryString = query.Build();

        Assert.Equal("Price > 1000 and Classification = \"技術書\"", queryString);
    }
    [Fact]
    public void ToQueryString_OrExpression_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(b => b.Classification == "技術書" || b.Classification == "SF");
        var queryString = query.Build();

        Assert.Equal("Classification = \"技術書\" or Classification = \"SF\"", queryString);
    }
    [Fact]
    public void ToQueryString_DateTimeEquals_ReturnsCorrectQuery() {
        var targetDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new KintoneQuery<BookModel>().Where(b => b.ReleaseDate!.Value == targetDate);
        var queryString = query.Build();

        Assert.Equal("ReleaseDate = \"2024-01-01T00:00:00Z\"", queryString);
    }
    [Fact]
    public void ToQueryString_TimeOnlyEquals_ReturnsCorrectQuery() {
        var time = new TimeOnly(9, 30);
        var query = new KintoneQuery<BookModel>().Where(b => b.TimeField.Value == time);
        var queryString = query.Build();

        Assert.Equal("TimeField = \"09:30\"", queryString);
    }
    [Fact]
    public void ToQueryString_BooleanLikeExpression_ThrowsNotSupported() {
        var exception = Assert.Throws<NotSupportedException>(() => new KintoneQuery<BookModel>().Where(b => b.Title.Contains("test")));
        Assert.Contains("like", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void Where_SimpleEqualityCondition_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(x => x.Title == "C#");
        var result = query.Build();
        Assert.Equal("Title = \"C#\"", result);
    }
    [Fact]
    public void Where_ComplexAndOrCondition_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>().Where(x => x.Title == "C#" && x.Price > 1000);
        var result = query.Build();
        Assert.Equal("Title = \"C#\" and Price > 1000", result);
    }
    [Fact]
    public void OrderBy_ThenByDescending_WorksCorrectly() {
        var query = new KintoneQuery<BookModel>()
            .OrderBy(x => x.Title)
            .ThenByDescending(x => x.Price);
        var result = query.Build();
        Assert.Contains("order by Title asc, Price desc", result);
    }
    [Fact]
    public void WhereIdIn_AddsCorrectInClause() {
        var query = new KintoneQuery<BookModel>().WhereIdIn(["123", "456"]);
        var result = query.Build();
        Assert.Equal("$id in (\"123\", \"456\")", result);
    }
    [Fact]
    public void Build_WithOffset_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<KintoneException>(() => query.SetQuery("offset 10").Build());
        Assert.Contains("offset", ex.Message);
    }
    [Fact]
    public void Build_FieldNameContainsOffset_DoesNotThrowException() {
        // Arrange
        var query = new KintoneQuery<BookModel>();
        // 「OffsetIncludedField」は、"offset" を含むフィールド名として想定
        query.SetQuery("OffsetIncludedField = \"test\"");
        // Act & Assert: 例外はスローされないはず
        var result = query.Build();
        Assert.Equal("OffsetIncludedField = \"test\"", result);
    }
    [Fact]
    public void Query_Title_Equals_String() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "C#入門")
            .Build();
        Assert.Equal("Title = \"C#入門\"", query);
    }
    [Fact]
    public void Query_Price_GreaterThan_2000() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price > 2000)
            .Build();
        Assert.Equal("Price > 2000", query);
    }
    [Fact]
    public void Query_Classification_Equals_SF() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Classification == "SF")
            .Build();
        Assert.Equal("Classification = \"SF\"", query);
    }
    [Fact]
    public void Query_ReleaseDate_Before_2025_01_01() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.ReleaseDate! < new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        Assert.Equal("ReleaseDate < \"2025-01-01T00:00:00Z\"", query);
    }
    [Fact]
    public void Query_ReleaseDate_Before_2025_01_01_LocalTime() {
        var localDateTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var query = new KintoneQuery<BookModel> {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")
        }
        .Where(x => x.ReleaseDate < localDateTime)
        .Build();
        // 東京標準時 = UTC+9 → UTCでは2024-12-31T15:00:00Zになる
        Assert.Equal("ReleaseDate < \"2024-12-31T15:00:00Z\"", query);
    }
    [Fact]
    public void Query_MultiSelector_Any() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.MultiSelector.Any(s => new[] { "選択肢1", "選択肢2" }.Contains(s)))
            .Build();
        Assert.Equal("MultiSelector in (\"選択肢1\", \"選択肢2\")", query);
    }
    [Fact]
    public void Query_Combined_Conditions_With_Order_Limit() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price >= 1000)
            .And(x => x.Recommendation == "5")
            .OrderByDescending(x => x.DateField)
            .Limit(20)
            .Build();
        Assert.Equal("Price >= 1000 and Recommendation = \"5\" order by DateField desc limit 20", query);
    }
    [Fact]
    public void Query_OrderBy_ThenByDescending_Limit() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "村上春樹全集")
            .OrderBy(x => x.ReleaseDate)
            .ThenByDescending(x => x.Price)
            .Limit(10)
            .Build();
        Assert.Equal("Title = \"村上春樹全集\" order by ReleaseDate asc, Price desc limit 10", query);
    }
    [Fact]
    public void Query_OrderByMultipleFieldsAscending() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price > 1500)
            .OrderBy(x => x.ReleaseDate)
            .ThenBy(x => x.Title)
            .Build();
        Assert.Equal("Price > 1500 order by ReleaseDate asc, Title asc", query);
    }
    [Fact]
    public void Query_OrderByDescendingThenBy() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price <= 2000)
            .OrderByDescending(x => x.Price)
            .ThenBy(x => x.Title)
            .Build();
        Assert.Equal("Price <= 2000 order by Price desc, Title asc", query);
    }
    [Fact]
    public void Query_OrderByDescendingThenByDescending() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "物語シリーズ")
            .OrderByDescending(x => x.ReleaseDate)
            .ThenByDescending(x => x.Price)
            .Build();
        Assert.Equal("Title = \"物語シリーズ\" order by ReleaseDate desc, Price desc", query);
    }
    [Fact]
    public void Query_OrderByWithLimitOnly() {
        var query = new KintoneQuery<BookModel>()
            .OrderBy(x => x.ReleaseDate)
            .Limit(5)
            .Build();
        Assert.Equal("order by ReleaseDate asc limit 5", query);
    }
    [Fact]
    public void Query_WhereIdEquals_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .WhereIdEquals("abc123")
            .Build();

        Assert.Equal("$id=\"abc123\"", query);
    }
    [Fact]
    public void Query_WhereIdsEquals_MultipleIds() {
        var query = new KintoneQuery<BookModel>()
            .WhereIdsEquals(new[] { "a", "b", "c" })
            .Build();

        Assert.Equal("$id=\"a\" or $id=\"b\" or $id=\"c\"", query);
    }
    [Fact]
    public void Query_SetQuery_OverridesConditions() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Price > 1000)
            .SetQuery("CustomField = \"abc\"")
            .Build();

        Assert.Equal("CustomField = \"abc\"", query);
    }
    [Fact]
    public void Query_Empty_Build_ReturnsEmptyString() {
        var query = new KintoneQuery<BookModel>().Build();
        Assert.Equal(string.Empty, query);
    }
    [Fact]
    public void ToString_ReturnsSameAsBuild() {
        var query = new KintoneQuery<BookModel>()
            .Where(b => b.Title == "C#入門")
            .OrderBy(b => b.ID)
            .Limit(10);

        Assert.Equal(query.Build(), query.ToString());
    }

}
