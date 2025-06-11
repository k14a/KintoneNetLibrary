using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers;

public class KintoneQueryInTests {
    [Fact]
    public void In_IntList_CreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Price, [100, 200, 300])
            .Build();

        Assert.Equal("Price in (100, 200, 300)", query);
    }
    [Fact]
    public void In_StringList_CreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Title, ["C#", "Java", "Go"])
            .Build();

        Assert.Equal("Title in (\"C#\", \"Java\", \"Go\")", query);
    }
    [Fact]
    public void In_EmptyList_ThrowsArgumentException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.In(x => x.Title, []));

        Assert.Contains("値のリストが空です", ex.Message);
    }
}

public class KintoneQueryNotInTests {
    [Fact]
    public void NotIn_IntList_CreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Price, [0, 1, 999])
            .Build();

        Assert.Equal("Price not in (0, 1, 999)", query);
    }
    [Fact]
    public void NotIn_StringList_CreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Classification, new[] { "Horror", "Sci-Fi" })
            .Build();

        Assert.Equal("Classification not in (\"Horror\", \"Sci-Fi\")", query);
    }
}

public class KintoneQueryInOverloadTests {
    [Fact]
    public void In_IntParams_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Price, 100, 200, 300)
            .Build();

        Assert.Equal("Price in (100, 200, 300)", query);
    }
    [Fact]
    public void In_StringParams_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .In(x => x.Title, "A", "B", "C")
            .Build();

        Assert.Equal("Title in (\"A\", \"B\", \"C\")", query);
    }
    [Fact]
    public void In_EmptyParams_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() => query.In<int?>(x => x.Price /* nullable int */, new List<int?> { }));

        Assert.Contains("値のリストが空です", ex.Message);
    }
    [Fact]
    public void In_NullParams_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        int?[]? values = null;
        var ex = Assert.Throws<ArgumentNullException>(() => query.In(x => x.Price, values!));

        Assert.Contains("values", ex.ParamName);
    }
}

public class KintoneQueryNotInOverloadTests {
    [Fact]
    public void NotIn_IntParams_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Price, 100, 200, 300)
            .Build();

        Assert.Equal("Price not in (100, 200, 300)", query);
    }
    [Fact]
    public void NotIn_StringParams_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotIn(x => x.Title, "A", "B", "C")
            .Build();

        Assert.Equal("Title not in (\"A\", \"B\", \"C\")", query);
    }
    [Fact]
    public void NotIn_EmptyParams_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.NotIn<int?>(x => x.Price, new int?[] { }));

        Assert.Contains("値のリストが空です", ex.Message);
    }
    [Fact]
    public void NotIn_NullParams_ThrowsException() {
        var query = new KintoneQuery<BookModel>();
        string[]? values = null;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            query.NotIn(x => x.Title, values!));

        Assert.Contains("values", ex.ParamName);
    }
}
