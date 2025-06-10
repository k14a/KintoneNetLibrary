using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

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

}
