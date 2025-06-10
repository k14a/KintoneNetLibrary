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
        var query = new KintoneQuery<BookModel>(b => b.Title == "C#入門");
        var queryString = query.ToQueryString();

        Assert.Equal("Title = \"C#入門\"", queryString);
    }
    [Fact]
    public void ToQueryString_GreaterThanExpression_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>(b => b.Price > 1000);
        var queryString = query.ToQueryString();

        Assert.Equal("Price > 1000", queryString);
    }
    [Fact]
    public void ToQueryString_AndExpression_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>(b => b.Price > 1000 && b.Classification == "技術書");
        var queryString = query.ToQueryString();

        Assert.Equal("Price > 1000 and Classification = \"技術書\"", queryString);
    }
    [Fact]
    public void ToQueryString_OrExpression_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>(b => b.Classification == "技術書" || b.Classification == "SF");
        var queryString = query.ToQueryString();

        Assert.Equal("Classification = \"技術書\" or Classification = \"SF\"", queryString);
    }
    [Fact]
    public void ToQueryString_DateTimeEquals_ReturnsCorrectQuery() {
        var targetDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new KintoneQuery<BookModel>(b => b.ReleaseDate!.Value == targetDate);
        var queryString = query.ToQueryString();

        Assert.Equal("ReleaseDate = \"2024-01-01T00:00:00Z\"", queryString);
    }
    [Fact]
    public void ToQueryString_TimeOnlyEquals_ReturnsCorrectQuery() {
        var time = new TimeOnly(9, 30);
        var query = new KintoneQuery<BookModel>(b => b.TimeField.Value == time);
        var queryString = query.ToQueryString();

        Assert.Equal("TimeField = \"09:30\"", queryString);
    }
    [Fact]
    public void ToQueryString_BooleanLikeExpression_ThrowsNotSupported() {
        // Funcにすることで評価を遅延し、Assert.Throwsが例外をキャッチ可能にする
        Func<KintoneQuery<BookModel>> createQuery = () => new KintoneQuery<BookModel>(b => b.Title.Contains("test"));

        var exception = Assert.Throws<NotSupportedException>(createQuery);

        Assert.Contains("like", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void Where_SimpleEqualityCondition_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "C#");

        var result = query.Build();

        Assert.Equal("Title = \"C#\"", result);
    }
    [Fact]
    public void Where_ComplexAndOrCondition_ReturnsCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.Title == "C#" && x.Price > 1000);

        var result = query.Build();

        Assert.Equal("Title = \"C#\" and Price > 1000", result);
    }
    [Fact]
    public void OrderBy_ThenByDescending_WorksCorrectly() {
        var query = new KintoneQuery<BookModel>()
            .OrderBy(x => x.Title)
            .ThenByDescending(x => x.Price);

        var result = query.ToString();

        Assert.Contains("order by Title asc, Price desc", result);
    }
    [Fact]
    public void WhereIdIn_AddsCorrectInClause() {
        var query = new KintoneQuery<BookModel>()
            .WhereIdIn(["123", "456"]);

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
            .Where(x => x.ReleaseDate! < new DateTime(2025, 1, 1))
            .Build();

        Assert.Equal("ReleaseDate < \"2025-01-01T00:00:00Z\"", query);
    }
    [Fact]
    public void Query_MultiSelector_Any() {
        var query = new KintoneQuery<BookModel>()
            .Where(x => x.MultiSelector.Any(s => new[] { "選択肢1", "選択肢2" }.Contains(s)))
            .Build();

        Assert.Equal("MultiSelector in (\"選択肢1\", \"選択肢2\")", query);
    }

    // [Fact]
    // public void Query_Combined_Conditions_With_Order_Limit() {
    //     var query = new KintoneQuery<BookModel>()
    //         .Where(x => x.Price >= 1000)
    //         .And(x => x.Recommendation == "5：強く勧めたい")
    //         .OrderByDescending(x => x.DateField)
    //         .Limit(20)
    //         .Build();

    //     Assert.Equal("Price >= 1000 and Recommendation = \"5：強く勧めたい\" order by DateField desc limit 20", query);
    // }

}
