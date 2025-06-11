using System;
using System.Linq;
using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Tests.Models;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers;

public class KintoneQueryBetweenTests {
    [Fact]
    public void Between_InclusiveIntRange_CreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .Between("Price", 100, 200)
            .Build();

        Assert.Equal("Price >= 100 and Price <= 200", query);
    }
    [Fact]
    public void Between_InclusiveDateRange_CreatesCorrectQuery() {
        var from = new DateTime(2023, 1, 1);
        var to = new DateTime(2023, 12, 31);

        var query = new KintoneQuery<BookModel>()
            .Between("PublishDate", from, to)
            .Build();

        Assert.Equal($"PublishDate >= \"{from:yyyy-MM-ddTHH:mm:ssZ}\" and PublishDate <= \"{to:yyyy-MM-ddTHH:mm:ssZ}\"", query);
    }
    [Fact]
    public void Between_FromGreaterThanTo_ThrowsArgumentException() {
        var ex = Assert.Throws<ArgumentException>(() => new KintoneQuery<BookModel>().Between("Price", 300, 100));

        Assert.Contains("from must be less than or equal to to", ex.Message);
    }
    [Fact]
    public void Between_FieldNameIsNull_ThrowsArgumentException() {
        var ex = Assert.Throws<ArgumentException>(() =>
            new KintoneQuery<BookModel>().Between(null!, 1, 10));

        Assert.Contains("Field name must be specified", ex.Message);
    }
    [Fact]
    public void Between_NullFromOrTo_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => new KintoneQuery<BookModel>().Between("Price", null!, 10));
        Assert.Throws<ArgumentNullException>(() => new KintoneQuery<BookModel>().Between("Price", 1, null!));
    }
    [Fact]
    public void Between_FromToTypeMismatch_ThrowsArgumentException() {
        var ex = Assert.Throws<ArgumentException>(() =>
            new KintoneQuery<BookModel>().Between("Price", 1, 1.5));

        Assert.Contains("from（Int32）と to（Double）の型は一致している必要があります", ex.Message);
    }
    [Fact]
    public void Between_UnsupportedType_ThrowsNotSupportedException() {
        var ex = Assert.Throws<NotSupportedException>(() =>
            new KintoneQuery<BookModel>().Between("UnsupportedField", new object(), new object()));

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能です", ex.Message);
    }
}

public class KintoneQueryBetweenExclusiveTests {
    [Fact]
    public void BetweenExclusive_IntRange_CreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .BetweenExclusive("Price", 100, 200)
            .Build();

        Assert.Equal("Price > 100 and Price < 200", query);
    }
    [Fact]
    public void BetweenExclusive_DateRange_CreatesCorrectQuery() {
        var from = new DateTime(2023, 1, 1);
        var to = new DateTime(2023, 12, 31);

        var query = new KintoneQuery<BookModel>()
            .BetweenExclusive("CreatedDate", from, to)
            .Build();

        Assert.Equal($"CreatedDate > \"{from:yyyy-MM-ddTHH:mm:ssZ}\" and CreatedDate < \"{to:yyyy-MM-ddTHH:mm:ssZ}\"", query);
    }
    [Fact]
    public void BetweenExclusive_InvalidField_ThrowsNotSupportedException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<NotSupportedException>(() =>
            query.BetweenExclusive("Title", "A", "Z"));  // 文字列は対象外

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能です", ex.Message);
    }
    [Fact]
    public void BetweenExclusive_FromGreaterThanTo_ThrowsArgumentException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.BetweenExclusive("Price", 200, 100));  // from > to

        Assert.Contains("from must be less than or equal to to", ex.Message);
    }
    [Fact]
    public void BetweenExclusive_TypeMismatch_ThrowsArgumentException() {
        var query = new KintoneQuery<BookModel>();
        var ex = Assert.Throws<ArgumentException>(() =>
            query.BetweenExclusive("Price", 100, 200.5));  // intとdoubleの型不一致

        Assert.Contains("型は一致している必要があります", ex.Message);
    }
}

public class KintoneQueryNotBetweenTests {
    [Fact]
    public void NotBetween_IntRange_CreatesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .NotBetween(x => x.Price, 100, 200)
            .Build();

        Assert.Equal("Price < 100 or Price > 200", query);
    }
    [Fact]
    public void NotBetween_DateTimeRange_CreatesCorrectQuery() {
        var from = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .NotBetween<DateTime?>(x => x.ReleaseDate, from, to)
            .Build();

        var expected = $"ReleaseDate < \"{from:yyyy-MM-ddTHH:mm:ssZ}\" or ReleaseDate > \"{to:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }
    [Fact]
    public void NotBetween_NullSelector_ThrowsArgumentNullException() {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new KintoneQuery<BookModel>().NotBetween<int>(null!, 1, 10));

        Assert.Equal("Value cannot be null. (Parameter 'fieldSelector')", ex.Message);
    }
    [Fact]
    public void NotBetween_UnsupportedType_ThrowsNotSupportedException() {
        var ex = Assert.Throws<NotSupportedException>(() =>
            new KintoneQuery<BookModel>().NotBetween(x => x.Title, "A", "Z"));  // Title is string

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能", ex.Message);
    }
    [Fact]
    public void NotBetween_FromGreaterThanTo_DoesNotThrow() {
        // NotBetween では from > to も特に問題とはしない（条件: x < from || x > to ）
        var query = new KintoneQuery<BookModel>()
            .NotBetween(x => x.Price, 200, 100)
            .Build();

        Assert.Equal("Price < 200 or Price > 100", query);
    }
}

public class KintoneQueryGreaterThanTests {
    [Fact]
    public void GreaterThan_IntValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThan(x => x.Price, 100)
            .Build();

        Assert.Equal("Price > 100", query);
    }
    [Fact]
    public void GreaterThan_DateTimeValue_GeneratesCorrectQuery() {
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .GreaterThan<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        Assert.Equal($"ReleaseDate > \"{date:yyyy-MM-ddTHH:mm:ssZ}\"", query);
    }
    [Fact]
    public void GreaterThan_ThrowsOnUnsupportedType() {
        var ex = Assert.Throws<NotSupportedException>(() => {
            var query = new KintoneQuery<BookModel>()
                .GreaterThan(x => x.Title, "Z")
                .Build();
        });

        Assert.Contains("この操作は数値型や日付型フィールドでのみ使用可能", ex.Message);
    }
    [Fact]
    public void GreaterThan_NullableInt_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThan(x => x.Price, (int?)150)
            .Build();

        Assert.Equal("Price > 150", query);
    }
}

public class KintoneQueryGreaterThanOrEqualTests {
    [Fact]
    public void GreaterThanOrEqual_IntValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThanOrEqual(x => x.Price, 100)
            .Build();

        Assert.Equal("Price >= 100", query);
    }
    [Fact]
    public void GreaterThanOrEqual_DateTimeValue_GeneratesCorrectQuery() {
        var value = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .GreaterThanOrEqual<DateTime?>(x => x.ReleaseDate, value)
            .Build();

        var expected = $"ReleaseDate >= \"{value:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }
    [Fact]
    public void GreaterThanOrEqual_DecimalValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .GreaterThanOrEqual(x => x.Rating, 4.5m)
            .Build();

        Assert.Equal("Rating >= 4.5", query);
    }
}

public class KintoneQueryLessThanTests {
    [Fact]
    public void LessThan_IntValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThan(x => x.Price, 500)
            .Build();

        Assert.Equal("Price < 500", query);
    }
    [Fact]
    public void LessThan_DateTimeValue_GeneratesCorrectQuery() {
        var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .LessThan<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        var expected = $"ReleaseDate < \"{date:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }
    [Fact]
    public void LessThan_DecimalValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThan(x => x.Rating, 3.5m)
            .Build();

        Assert.Equal("Rating < 3.5", query);
    }
}

public class KintoneQueryLessThanOrEqualTests {
    [Fact]
    public void LessThanOrEqual_IntValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThanOrEqual(x => x.Price, 500)
            .Build();

        Assert.Equal("Price <= 500", query);
    }
    [Fact]
    public void LessThanOrEqual_DateTimeValue_GeneratesCorrectQuery() {
        var date = new DateTime(2025, 6, 11, 12, 0, 0, DateTimeKind.Utc);

        var query = new KintoneQuery<BookModel>()
            .LessThanOrEqual<DateTime?>(x => x.ReleaseDate, date)
            .Build();

        var expected = $"ReleaseDate <= \"{date:yyyy-MM-ddTHH:mm:ssZ}\"";
        Assert.Equal(expected, query);
    }
    [Fact]
    public void LessThanOrEqual_DecimalValue_GeneratesCorrectQuery() {
        var query = new KintoneQuery<BookModel>()
            .LessThanOrEqual(x => x.Rating, 4.5m)
            .Build();

        Assert.Equal("Rating <= 4.5", query);
    }
}