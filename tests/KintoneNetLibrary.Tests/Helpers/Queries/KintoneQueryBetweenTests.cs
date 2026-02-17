using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

/// <summary>
/// KintoneQueryのBetween、BetweenExclusive、NotBetween、GreaterThan、GreaterThanOrEqual、LessThan、LessThanOrEqualの各メソッドの動作をテストするクラスです。数値型や日付型フィールドに対して正しいクエリが生成されることを検証し、無効な引数やサポートされていないフィールドタイプに対して適切な例外がスローされることを確認します。
/// </summary>
public class KintoneQueryBetweenTests {
    /// <summary>
    /// Betweenメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対して100から200の範囲を指定し、生成されるクエリが"Price >= 100 and Price <= 200"であることを検証します。
    /// </summary>
    [Fact]
    public void BetweenInclusiveIntRangeCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Between("Price", 100, 200)
            .Build();

        Assert.Equal("Price >= 100 and Price <= 200", query);
    }

    /// <summary>
    /// Betweenメソッドが、日付型フィールドに対して正しいクエリを生成することをテストします。ReleaseDateフィールドに対して2023年1月1日から2023年12月31日までの範囲を指定し、生成されるクエリが"ReleaseDate >= "2023-01-01T00:00:00Z" and ReleaseDate <= "2023-12-31T00:00:00Z""であることを検証します。
    /// </summary>
    [Fact]
    public void BetweenInclusiveDateRangeCreatesCorrectQuery() {
        var from = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .Between("ReleaseDate", from, to)
            .Build();

        Assert.Equal($"ReleaseDate >= \"{from:yyyy-MM-ddTHH:mm:ssZ}\" and ReleaseDate <= \"{to:yyyy-MM-ddTHH:mm:ssZ}\"", query);
    }

    /// <summary>
    /// Betweenメソッドが、フィールド名がnullの場合にArgumentExceptionをスローすることをテストします。フィールド名にnullを指定してBetweenを呼び出し、"Field name must be specified"というメッセージを含むArgumentExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenFromGreaterThanToThrowsArgumentException() {
        var ex = Assert.Throws<ArgumentException>(() => new KintoneQuery<BookModel>().Between("Price", 300, 100));

        Assert.Contains("from must be less than or equal to to", ex.Message);
    }

    /// <summary>
    /// Betweenメソッドが、fromまたはtoのいずれかがnullの場合にArgumentNullExceptionをスローすることをテストします。fromにnullを指定してBetweenを呼び出し、toにnullを指定してBetweenを呼び出し、それぞれArgumentNullExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenFieldNameIsNullThrowsArgumentException() {
        var ex = Assert.Throws<ArgumentException>(() =>
            new KintoneQuery<BookModel>().Between(null!, 1, 10));

        Assert.Contains("Field name must be specified", ex.Message);
    }

    /// <summary>
    /// Betweenメソッドが、fromとtoの型が一致していない場合にArgumentExceptionをスローすることをテストします。fromにint、toにdoubleを指定してBetweenを呼び出し、"from（Int32）と to（Double）の型は一致している必要があります"というメッセージを含むArgumentExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenNullFromOrToThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => new KintoneQuery<BookModel>().Between("Price", null!, 10));
        Assert.Throws<ArgumentNullException>(() => new KintoneQuery<BookModel>().Between("Price", 1, null!));
    }

    /// <summary>
    /// Betweenメソッドが、fromとtoの型が一致していない場合にArgumentExceptionをスローすることをテストします。fromにint、toにdoubleを指定してBetweenを呼び出し、"from（Int32）と to（Double）の型は一致している必要があります"というメッセージを含むArgumentExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenFromToTypeMismatchThrowsArgumentException() {
        var ex = Assert.Throws<ArgumentException>(() =>
            new KintoneQuery<BookModel>().Between("Price", 1, 1.5));

        Assert.Contains("from（Int32）と to（Double）の型は一致している必要があります", ex.Message);
    }

    /// <summary>
    /// Betweenメソッドが、数値型や日付型以外のフィールドに対して呼び出された場合にNotSupportedExceptionをスローすることをテストします。Titleフィールド（文字列型）に対してBetweenを呼び出し、"この操作は数値型や日付型フィールドでのみ使用可能です"というメッセージを含むNotSupportedExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenUnsupportedTypeThrowsNotSupportedException() {
        var ex = Assert.Throws<NotSupportedException>(() =>
            new KintoneQuery<BookModel>().Between("UnsupportedField", new object(), new object()));

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能です", ex.Message);
    }
}

/// <summary>
/// KintoneQueryのBetweenExclusiveメソッドの動作をテストするクラスです。数値型や日付型フィールドに対して正しいクエリが生成されることを検証し、無効な引数やサポートされていないフィールドタイプに対して適切な例外がスローされることを確認します。
/// </summary>
public class KintoneQueryBetweenExclusiveTests {
    /// <summary>
    /// BetweenExclusiveメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対して100から200の範囲を指定し、生成されるクエリが"Price > 100 and Price < 200"であることを検証します。
    /// </summary>
    [Fact]
    public void BetweenExclusiveIntRangeCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .BetweenExclusive("Price", 100, 200)
            .Build();

        Assert.Equal("Price > 100 and Price < 200", query);
    }

    /// <summary>
    /// BetweenExclusiveメソッドが、日付型フィールドに対して正しいクエリを生成することをテストします。ReleaseDateフィールドに対して2023年1月1日から2023年12月31日までの範囲を指定し、生成されるクエリが"ReleaseDate > "2023-01-01T00:00:00Z" and ReleaseDate < "2023-12-31T00:00:00Z""であることを検証します。
    /// </summary>
    [Fact]
    public void BetweenExclusiveDateRangeCreatesCorrectQuery() {
        var from = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .BetweenExclusive("ReleaseDate", from, to)
            .Build();

        Assert.Equal($"ReleaseDate > \"{from:yyyy-MM-ddTHH:mm:ssZ}\" and ReleaseDate < \"{to:yyyy-MM-ddTHH:mm:ssZ}\"", query);
    }

    /// <summary>
    /// BetweenExclusiveメソッドが、数値型や日付型以外のフィールドに対して呼び出された場合にNotSupportedExceptionをスローすることをテストします。Titleフィールド（文字列型）に対してBetweenExclusiveを呼び出し、"この操作は数値型や日付型フィールドでのみ使用可能です"というメッセージを含むNotSupportedExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenExclusiveInvalidFieldThrowsNotSupportedException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<NotSupportedException>(() =>
            query.BetweenExclusive("Title", "A", "Z"));  // 文字列は対象外

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能です", ex.Message);
    }

    /// <summary>
    /// BetweenExclusiveメソッドが、fromの値がtoの値より大きい場合にArgumentExceptionをスローすることをテストします。Priceフィールドに対してfromに200、toに100を指定してBetweenExclusiveを呼び出し、"from must be less than or equal to to"というメッセージを含むArgumentExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenExclusiveFromGreaterThanToThrowsArgumentException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.BetweenExclusive("Price", 200, 100));  // from > to

        Assert.Contains("from must be less than or equal to to", ex.Message);
    }

    /// <summary>
    /// BetweenExclusiveメソッドが、fromとtoの型が一致していない場合にArgumentExceptionをスローすることをテストします。Priceフィールドに対してfromにint、toにdoubleを指定してBetweenExclusiveを呼び出し、"from（Int32）と to（Double）の型は一致している必要があります"というメッセージを含むArgumentExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void BetweenExclusiveTypeMismatchThrowsArgumentException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.BetweenExclusive("Price", 100, 200.5));  // intとdoubleの型不一致

        Assert.Contains("型は一致している必要があります", ex.Message);
    }
}

/// <summary>
/// KintoneQueryのNotBetweenメソッドの動作をテストするクラスです。数値型や日付型フィールドに対して正しいクエリが生成されることを検証し、無効な引数やサポートされていないフィールドタイプに対して適切な例外がスローされることを確認します。
/// </summary>
public class KintoneQueryNotBetweenTests {
    /// <summary>
    /// NotBetweenメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対して100から200の範囲を指定し、生成されるクエリが"Price < 100 or Price > 200"であることを検証します。
    /// </summary>
    [Fact]
    public void NotBetweenIntRangeCreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotBetween(x => x.Price, 100, 200)
            .Build();

        Assert.Equal("Price < 100 or Price > 200", query);
    }

    /// <summary>
    /// NotBetweenメソッドが、日付型フィールドに対して正しいクエリを生成することをテストします。ReleaseDateフィールドに対して2023年1月1日から2023年12月31日までの範囲を指定し、生成されるクエリが"ReleaseDate < "2023-01-01T00:00:00Z" or ReleaseDate > "2023-12-31T23:59:59Z""であることを検証します。
    /// </summary>
    [Fact]
    public void NotBetweenDateTimeRangeCreatesCorrectQuery() {
        var from = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .NotBetween<DateTime?>(x => x.ReleaseDate, from, to)
            .Build();

        var expected = $"ReleaseDate < \"{from:yyyy-MM-ddTHH:mm:ssZ}\" or ReleaseDate > \"{to:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }

    /// <summary>
    /// NotBetweenメソッドが、フィールドセレクターがnullの場合にArgumentNullExceptionをスローすることをテストします。フィールドセレクターにnullを指定してNotBetweenを呼び出し、"Value cannot be null. (Parameter 'fieldSelector')"というメッセージを含むArgumentNullExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void NotBetweenNullSelectorThrowsArgumentNullException() {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new KintoneQuery<BookModel>().NotBetween<int>(null!, 1, 10));

        Assert.Equal("Value cannot be null. (Parameter 'fieldSelector')", ex.Message);
    }

    /// <summary>
    /// NotBetweenメソッドが、fromの値がtoの値より大きい場合にArgumentExceptionをスローすることをテストします。Priceフィールドに対してfromに200、toに100を指定してNotBetweenを呼び出し、"from must be less than or equal to to"というメッセージを含むArgumentExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void NotBetweenUnsupportedTypeThrowsNotSupportedException() {
        var ex = Assert.Throws<NotSupportedException>(() =>
            new KintoneQuery<BookModel>().NotBetween(x => x.Title, "A", "Z"));  // Title is string

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能", ex.Message);
    }

    /// <summary>
    /// NotBetweenメソッドが、fromの値がtoの値より大きい場合にArgumentExceptionをスローすることをテストします。Priceフィールドに対してfromに200、toに100を指定してNotBetweenを呼び出し、"from must be less than or equal to to"というメッセージを含むArgumentExceptionがスローされることを検証します。
    /// </summary>
    [Fact]
    public void NotBetweenFromGreaterThanToDoesNotThrow() {
        // NotBetween では from > to も特に問題とはしない（条件: x < from || x > to ）
        var query = new KintoneQuery<BookModel>()
            .NotBetween(x => x.Price, 200, 100)
            .Build();

        Assert.Equal("Price < 200 or Price > 100", query);
    }
}

/// <summary>
/// KintoneQueryのGreaterThan、GreaterThanOrEqual、LessThan、LessThanOrEqualの各メソッドの動作をテストするクラスです。数値型や日付型フィールドに対して正しいクエリが生成されることを検証し、無効な引数やサポートされていないフィールドタイプに対して適切な例外がスローされることを確認します。
/// </summary>
public class KintoneQueryGreaterThanTests {
    /// <summary>
    /// GreaterThanメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対して100の値を指定し、生成されるクエリが"Price > 100"であることを検証します。
    /// </summary>
    [Fact]
    public void GreaterThanIntValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThan(x => x.Price, 100)
            .Build();

        Assert.Equal("Price > 100", query);
    }

    /// <summary>
    /// GreaterThanメソッドが、日付型フィールドに対して正しいクエリを生成することをテストします。ReleaseDateフィールドに対して2024年1月1日の値を指定し、生成されるクエリが"ReleaseDate > "2024-01-01T00:00:00Z""であることを検証します。
    /// </summary>
    [Fact]
    public void GreaterThanDateTimeValueGeneratesCorrectQuery() {
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .GreaterThan<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        Assert.Equal($"ReleaseDate > \"{date:yyyy-MM-ddTHH:mm:ssZ}\"", query);
    }

    /// <summary>
    /// GreaterThanメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Ratingフィールドに対して4.5の値を指定し、生成されるクエリが"Rating > 4.5"であることを検証します。
    /// </summary>
    [Fact]
    public void GreaterThanThrowsOnUnsupportedType() {
        var ex = Assert.Throws<NotSupportedException>(() => {
            var query = new KintoneQuery<BookModel>()
                .GreaterThan(x => x.Title, "Z")
                .Build();
        });

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能", ex.Message);
    }

    /// <summary>
    /// GreaterThanメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対してnullの値を指定し、生成されるクエリが"Price > 150"であることを検証します。
    /// </summary>
    [Fact]
    public void GreaterThanNullableIntGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThan(x => x.Price, (int?)150)
            .Build();

        Assert.Equal("Price > 150", query);
    }
}

/// <summary>
/// KintoneQueryのGreaterThanOrEqualメソッドが、数値型や日付型フィールドに対して正しいクエリを生成することをテストするクラスです。Priceフィールドに対して100の値を指定した場合、生成されるクエリが"Price >= 100"であることを検証します。また、ReleaseDateフィールドに対して2024年1月1日の値を指定した場合、生成されるクエリが"ReleaseDate >= "2024-01-01T00:00:00Z""であることを検証します。さらに、Ratingフィールドに対して4.5の値を指定した場合、生成されるクエリが"Rating >= 4.5"であることを検証します。
/// </summary>
public class KintoneQueryGreaterThanOrEqualTests {
    /// <summary>
    /// GreaterThanOrEqualメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対して100の値を指定し、生成されるクエリが"Price >= 100"であることを検証します。
    /// </summary>
    [Fact]
    public void GreaterThanOrEqualIntValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThanOrEqual(x => x.Price, 100)
            .Build();

        Assert.Equal("Price >= 100", query);
    }

    /// <summary>
    /// GreaterThanOrEqualメソッドが、日付型フィールドに対して正しいクエリを生成することをテストします。ReleaseDateフィールドに対して2024年1月1日の値を指定し、生成されるクエリが"ReleaseDate >= "2024-01-01T00:00:00Z""であることを検証します。
    /// </summary>
    [Fact]
    public void GreaterThanOrEqualDateTimeValueGeneratesCorrectQuery() {
        var value = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .GreaterThanOrEqual<DateTime?>(x => x.ReleaseDate, value)
            .Build();

        var expected = $"ReleaseDate >= \"{value:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }

    /// <summary>
    /// GreaterThanOrEqualメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Ratingフィールドに対して4.5の値を指定し、生成されるクエリが"Rating >= 4.5"であることを検証します。
    /// </summary>
    [Fact]
    public void GreaterThanOrEqualDecimalValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThanOrEqual(x => x.Rating, 4.5m)
            .Build();

        Assert.Equal("Rating >= 4.5", query);
    }
}

/// <summary>
/// KintoneQueryのLessThan、LessThanOrEqualメソッドが、数値型や日付型フィールドに対して正しいクエリを生成することをテストするクラスです。Priceフィールドに対して100の値を指定した場合、生成されるクエリが"Price < 100"であることを検証します。また、ReleaseDateフィールドに対して2024年1月1日の値を指定した場合、生成されるクエリが"ReleaseDate < "2024-01-01T00:00:00Z""であることを検証します。さらに、Ratingフィールドに対して4.5の値を指定した場合、生成されるクエリが"Rating < 4.5"であることを検証します。
/// </summary>
public class KintoneQueryLessThanTests {
    /// <summary>
    /// LessThanメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対して100の値を指定し、生成されるクエリが"Price < 100"であることを検証します。
    /// </summary>
    [Fact]
    public void LessThanIntValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThan(x => x.Price, 500)
            .Build();

        Assert.Equal("Price < 500", query);
    }

    /// <summary>
    /// LessThanメソッドが、日付型フィールドに対して正しいクエリを生成することをテストします。ReleaseDateフィールドに対して2025年1月1日の値を指定し、生成されるクエリが"ReleaseDate < "2025-01-01T00:00:00Z""であることを検証します。
    /// </summary>
    [Fact]
    public void LessThanDateTimeValueGeneratesCorrectQuery() {
        var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .LessThan<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        var expected = $"ReleaseDate < \"{date:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }

    /// <summary>
    /// LessThanメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Ratingフィールドに対して4.5の値を指定し、生成されるクエリが"Rating < 4.5"であることを検証します。
    /// </summary>
    [Fact]
    public void LessThanDecimalValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThan(x => x.Rating, 3.5m)
            .Build();

        Assert.Equal("Rating < 3.5", query);
    }
}

/// <summary>
/// KintoneQueryのLessThanOrEqualメソッドが、数値型や日付型フィールドに対して正しいクエリを生成することをテストするクラスです。Priceフィールドに対して100の値を指定した場合、生成されるクエリが"Price <= 100"であることを検証します。また、ReleaseDateフィールドに対して2024年1月1日の値を指定した場合、生成されるクエリが"ReleaseDate <= "2024-01-01T00:00:00Z""であることを検証します。さらに、Ratingフィールドに対して4.5の値を指定した場合、生成されるクエリが"Rating <= 4.5"であることを検証します。
/// </summary>
public class KintoneQueryLessThanOrEqualTests {
    /// <summary>
    /// LessThanOrEqualメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Priceフィールドに対して100の値を指定し、生成されるクエリが"Price <= 100"であることを検証します。
    /// </summary>
    [Fact]
    public void LessThanOrEqualIntValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThanOrEqual(x => x.Price, 500)
            .Build();

        Assert.Equal("Price <= 500", query);
    }

    /// <summary>
    /// LessThanOrEqualメソッドが、日付型フィールドに対して正しいクエリを生成することをテストします。ReleaseDateフィールドに対して2025年6月11日の値を指定し、生成されるクエリが"ReleaseDate <= "2025-06-11T12:00:00Z""であることを検証します。
    /// </summary>
    [Fact]
    public void LessThanOrEqualDateTimeValueGeneratesCorrectQuery() {
        var date = new DateTime(2025, 6, 11, 12, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .LessThanOrEqual<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        var expected = $"ReleaseDate <= \"{date:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }

    /// <summary>
    /// LessThanOrEqualメソッドが、数値型フィールドに対して正しいクエリを生成することをテストします。Ratingフィールドに対して4.5の値を指定し、生成されるクエリが"Rating <= 4.5"であることを検証します。
    /// </summary>
    [Fact]
    public void LessThanOrEqualDecimalValueGeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThanOrEqual(x => x.Rating, 4.5m)
            .Build();

        Assert.Equal("Rating <= 4.5", query);
    }
}