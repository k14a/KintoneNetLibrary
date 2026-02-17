using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

/// <summary>
/// KintoneQueryのEqualとNotEqualメソッドのテストクラスです。
/// </summary>
public class KintoneQueryEqualTests {
    /// <summary>
    /// Equalメソッドが整数値に対して正しいクエリを生成することをテストします。
    /// </summary>
    [Fact]
    public void EqualIntValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Equal(x => x.Price, 100)
            .Build();

        Assert.Equal("Price = 100", query);
    }

    /// <summary>
    /// Equalメソッドが文字列値に対して正しいクエリを生成することをテストします。
    /// </summary>
    [Fact]
    public void EqualStringValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Equal(x => x.Title, "C# Guide")
            .Build();

        Assert.Equal("Title = \"C# Guide\"", query);
    }

    /// <summary>
    /// EqualメソッドがDateTime値に対して正しいクエリを生成することをテストします。
    /// </summary>
    [Fact]
    public void NotEqualDateTimeValueGeneratesCorrectQuery() {
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new KintoneQuery<BookModel>()
            .NotEqual<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        var expected = $"ReleaseDate != \"{date:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }

    /// <summary>
    /// NotEqualメソッドがBoolean値に対して正しいクエリを生成することをテストします。
    /// </summary>
    [Fact]
    public void NotEqualBooleanValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotEqual(x => x.IgnoreRevision, true)
            .Build();

        Assert.Equal("IgnoreRevision != true", query);
    }
}

