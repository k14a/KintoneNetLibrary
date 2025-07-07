using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

public class KintoneQueryEqualTests {
    [Fact]
    public void Equal_IntValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Equal(x => x.Price, 100)
            .Build();

        Assert.Equal("Price = 100", query);
    }
    [Fact]
    public void Equal_StringValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Equal(x => x.Title, "C# Guide")
            .Build();

        Assert.Equal("Title = \"C# Guide\"", query);
    }
    [Fact]
    public void NotEqual_DateTimeValue_GeneratesCorrectQuery() {
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new KintoneQuery<BookModel>()
            .NotEqual<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        var expected = $"ReleaseDate != \"{date:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }
    [Fact]
    public void NotEqual_BooleanValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotEqual(x => x.IgnoreRevision, true)
            .Build();

        Assert.Equal("IgnoreRevision != true", query);
    }
}

