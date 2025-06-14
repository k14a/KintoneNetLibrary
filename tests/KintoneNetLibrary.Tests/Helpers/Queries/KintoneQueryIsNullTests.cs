using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

public class KintoneQueryNullTests {
    [Fact]
    public void IsNull_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNull(x => x.Price)
            .Build();

        Assert.Equal("Price = null", query);
    }
    [Fact]
    public void IsNotNull_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNotNull(x => x.Title)
            .Build();

        Assert.Equal("Title != null", query);
    }
    [Fact]
    public void IsNull_NullExpression_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        Assert.Throws<ArgumentNullException>(() => query.IsNull<string>(null!));
    }
    [Fact]
    public void IsNotNull_NullExpression_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        Assert.Throws<ArgumentNullException>(() => query.IsNotNull<string>(null!));
    }
}

public class KintoneQueryIsNullStringFieldTests {
    [Fact]
    public void IsNull_StringField_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNull("Title")
            .Build();

        Assert.Equal("Title = null", query);
    }
    [Fact]
    public void IsNotNull_StringField_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNotNull("Title")
            .Build();

        Assert.Equal("Title != null", query);
    }
    [Fact]
    public void IsNull_EmptyField_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() => query.IsNull(""));

        Assert.Contains("field", ex.Message);
    }
    [Fact]
    public void IsNotNull_NullField_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentNullException>(() => query.IsNotNull(null!));

        Assert.Contains("field", ex.Message);
    }
}
