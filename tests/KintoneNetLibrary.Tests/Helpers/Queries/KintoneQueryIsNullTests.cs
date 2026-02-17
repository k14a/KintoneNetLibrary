using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

/// <summary>
/// KintoneQueryのIsNullとIsNotNullのテストクラス。
/// </summary>
public class KintoneQueryNullTests {
    /// <summary>
    /// IsNullメソッドがnullを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void IsNullGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNull(x => x.Price)
            .Build();

        Assert.Equal("Price = null", query);
    }

    /// <summary>
    /// IsNotNullメソッドがnullを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void IsNotNullGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNotNull(x => x.Title)
            .Build();

        Assert.Equal("Title != null", query);
    }

    /// <summary>
    /// IsNullメソッドにnullの式を渡した場合、ArgumentNullExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void IsNullNullExpressionThrowsException() {
        var query = new KintoneQuery<BookModel>();
        Assert.Throws<ArgumentNullException>(() => query.IsNull<string>(null!));
    }

    /// <summary>
    /// IsNotNullメソッドにnullの式を渡した場合、ArgumentNullExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void IsNotNullNullExpressionThrowsException() {
        var query = new KintoneQuery<BookModel>();
        Assert.Throws<ArgumentNullException>(() => query.IsNotNull<string>(null!));
    }
}

/// <summary>
/// KintoneQueryのIsNullとIsNotNullの文字列フィールドに対するテストクラス。
/// </summary>
public class KintoneQueryIsNullStringFieldTests {
    /// <summary>
    /// IsNullメソッドが文字列フィールドに対してnullを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void IsNullStringFieldGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNull("Title")
            .Build();

        Assert.Equal("Title = null", query);
    }

    /// <summary>
    /// IsNotNullメソッドが文字列フィールドに対してnullを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void IsNotNullStringFieldGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .IsNotNull("Title")
            .Build();

        Assert.Equal("Title != null", query);
    }

    /// <summary>
    /// IsNullメソッドに空のフィールド名を渡した場合、ArgumentExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void IsNullEmptyFieldThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() => query.IsNull(""));

        Assert.Contains("field", ex.Message);
    }

    /// <summary>
    /// IsNotNullメソッドに空のフィールド名を渡した場合、ArgumentExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void IsNotNullNullFieldThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentNullException>(() => query.IsNotNull(null!));

        Assert.Contains("field", ex.Message);
    }
}
