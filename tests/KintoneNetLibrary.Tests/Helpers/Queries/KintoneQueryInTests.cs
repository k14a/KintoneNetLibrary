using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

public class KintoneQueryInTests {
    [Fact]
    public void InIntListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Price, [100, 200, 300])
            .Build();

        Assert.Equal("Price in (100, 200, 300)", query);
    }
    [Fact]
    public void InStringListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Title, ["C#", "Java", "Go"])
            .Build();

        Assert.Equal("Title in (\"C#\", \"Java\", \"Go\")", query);
    }
    [Fact]
    public void InEmptyListThrowsArgumentException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.In(x => x.Title, []));

        Assert.Contains("値のリストが空です", ex.Message);
    }
}

public class KintoneQueryNotInTests {
    [Fact]
    public void NotInIntListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Price, [0, 1, 999])
            .Build();

        Assert.Equal("Price not in (0, 1, 999)", query);
    }
    [Fact]
    public void NotInStringListCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Classification, new[] { "Horror", "Sci-Fi" })
            .Build();

        Assert.Equal("Classification not in (\"Horror\", \"Sci-Fi\")", query);
    }
}

public class KintoneQueryInOverloadTests {
    [Fact]
    public void InIntParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Price, 100, 200, 300)
            .Build();

        Assert.Equal("Price in (100, 200, 300)", query);
    }
    [Fact]
    public void InStringParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Title, "A", "B", "C")
            .Build();

        Assert.Equal("Title in (\"A\", \"B\", \"C\")", query);
    }
    [Fact]
    public void InEmptyParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() => query.In<int?>(x => x.Price /* nullable int */, new List<int?> { }));

        Assert.Contains("値のリストが空です", ex.Message);
    }
    [Fact]
    public void InNullParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        int?[]? values = null;
        var ex = Assert.Throws<ArgumentNullException>(() => query.In(x => x.Price, values!));

        Assert.Contains("values", ex.ParamName);
    }
}

public class KintoneQueryNotInOverloadTests {
    [Fact]
    public void NotInIntParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Price, 100, 200, 300)
            .Build();

        Assert.Equal("Price not in (100, 200, 300)", query);
    }
    [Fact]
    public void NotInStringParamsGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Title, "A", "B", "C")
            .Build();

        Assert.Equal("Title not in (\"A\", \"B\", \"C\")", query);
    }
    [Fact]
    public void NotInEmptyParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.NotIn<int?>(x => x.Price, new int?[] { }));

        Assert.Contains("値のリストが空です", ex.Message);
    }
    [Fact]
    public void NotInNullParamsThrowsException() {
        var query = new KintoneQuery<BookModel>();
        string[]? values = null;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            query.NotIn(x => x.Title, values!));

        Assert.Contains("values", ex.ParamName);
    }
}
