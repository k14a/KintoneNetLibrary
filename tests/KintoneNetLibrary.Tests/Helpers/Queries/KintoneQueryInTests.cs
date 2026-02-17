using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

/// <summary>
/// KintoneQueryのInとNotInのテストクラス。
/// </summary>
public class KintoneQueryInTests {
    /// <summary>
    /// Inメソッドが整数のリストを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void InIntListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Price, [100, 200, 300])
            .Build();

        Assert.Equal("Price in (100, 200, 300)", query);
    }

    /// <summary>
    /// Inメソッドが文字列のリストを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void InStringListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Title, ["C#", "Java", "Go"])
            .Build();

        Assert.Equal("Title in (\"C#\", \"Java\", \"Go\")", query);
    }

    /// <summary>
    /// Inメソッドに空のリストを渡した場合、ArgumentExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void InEmptyListThrowsArgumentException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.In(x => x.Title, []));

        Assert.Contains("値のリストが空です", ex.Message);
    }
}

/// <summary>
/// KintoneQueryのNotInメソッドのテストクラス。
/// </summary>
public class KintoneQueryNotInTests {
    /// <summary>
    /// NotInメソッドが整数のリストを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void NotInIntListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Price, [0, 1, 999])
            .Build();

        Assert.Equal("Price not in (0, 1, 999)", query);
    }

    /// <summary>
    /// NotInメソッドが文字列のリストを正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void NotInStringListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Classification, new[] { "Horror", "Sci-Fi" })
            .Build();

        Assert.Equal("Classification not in (\"Horror\", \"Sci-Fi\")", query);
    }
}

/// <summary>
/// KintoneQueryのInとNotInのオーバーロードメソッドのテストクラス。
/// </summary>
public class KintoneQueryInOverloadTests {
    /// <summary>
    /// Inメソッドのオーバーロードが整数の可変引数を正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void InIntParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Price, 100, 200, 300)
            .Build();

        Assert.Equal("Price in (100, 200, 300)", query);
    }

    /// <summary>
    /// Inメソッドのオーバーロードが文字列の可変引数を正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void InStringParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Title, "A", "B", "C")
            .Build();

        Assert.Equal("Title in (\"A\", \"B\", \"C\")", query);
    }

    /// <summary>
    /// Inメソッドのオーバーロードに空のリストを渡した場合、ArgumentExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void InEmptyParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() => query.In<int?>(x => x.Price /* nullable int */, new List<int?> { }));

        Assert.Contains("値のリストが空です", ex.Message);
    }

    /// <summary>
    /// Inメソッドのオーバーロードにnullを渡した場合、ArgumentNullExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void InNullParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        int?[]? values = null;
        var ex = Assert.Throws<ArgumentNullException>(() => query.In(x => x.Price, values!));

        Assert.Contains("values", ex.ParamName);
    }
}

/// <summary>
/// KintoneQueryのNotInメソッドのオーバーロードのテストクラス。
/// </summary>
public class KintoneQueryNotInOverloadTests {
    /// <summary>
    /// NotInメソッドのオーバーロードが整数の可変引数を正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void NotInIntParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Price, 100, 200, 300)
            .Build();

        Assert.Equal("Price not in (100, 200, 300)", query);
    }

    /// <summary>
    /// NotInメソッドのオーバーロードが文字列の可変引数を正しいクエリ文字列に変換することをテストします。
    /// </summary>
    [Fact]
    public void NotInStringParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Title, "A", "B", "C")
            .Build();

        Assert.Equal("Title not in (\"A\", \"B\", \"C\")", query);
    }

    /// <summary>
    /// NotInメソッドのオーバーロードに空のリストを渡した場合、ArgumentExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void NotInEmptyParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.NotIn<int?>(x => x.Price, new int?[] { }));

        Assert.Contains("値のリストが空です", ex.Message);
    }

    /// <summary>
    /// NotInメソッドのオーバーロードにnullを渡した場合、ArgumentNullExceptionがスローされることをテストします。
    /// </summary>
    [Fact]
    public void NotInNullParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        string[]? values = null;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            query.NotIn(x => x.Title, values!));

        Assert.Contains("values", ex.ParamName);
    }
}
