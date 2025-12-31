using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

public class KintoneQueryNullTests {
    [Fact]
    public void IsNullGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNull(x => x.Price)
            .Build();

        Assert.Equal("Price = null", query);
    }
    [Fact]
    public void IsNotNullGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNotNull(x => x.Title)
            .Build();

        Assert.Equal("Title != null", query);
    }
    [Fact]
    public void IsNullNullExpressionThrowsException() {
        var query = new KintoneQuery<BookModel>();
        Assert.Throws<ArgumentNullException>(() => query.IsNull<string>(null!));
    }
    [Fact]
    public void IsNotNullNullExpressionThrowsException() {
        var query = new KintoneQuery<BookModel>();
        Assert.Throws<ArgumentNullException>(() => query.IsNotNull<string>(null!));
    }
}

public class KintoneQueryIsNullStringFieldTests {
    [Fact]
    public void IsNullStringFieldGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNull("Title")
            .Build();

        Assert.Equal("Title = null", query);
    }
    [Fact]
    public void IsNotNullStringFieldGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNotNull("Title")
            .Build();

        Assert.Equal("Title != null", query);
    }
    [Fact]
    public void IsNullEmptyFieldThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() => query.IsNull(""));

        Assert.Contains("field", ex.Message);
    }
    [Fact]
    public void IsNotNullNullFieldThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentNullException>(() => query.IsNotNull(null!));

        Assert.Contains("field", ex.Message);
    }
}
