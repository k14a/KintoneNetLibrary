using System;
using Xunit;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Tests.Types;

public class KintoneDateTimeTests {
    [Fact]
    public void ConstructorWithValidDateTimeSetsValueCorrectly() {
        var dateTime = new DateTime(2025, 6, 6, 14, 30, 0);
        var kintoneDateTime = new KintoneDateTime(dateTime);

        Assert.Equal(dateTime, kintoneDateTime.Value);
    }

    [Fact]
    public void ToStringReturnsExpectedKintoneFormat() {
        var dateTime = new DateTime(2025, 6, 6, 14, 30, 0);
        var kintoneDateTime = new KintoneDateTime(dateTime);

        var expected = "2025-06-06T14:30";
        Assert.Equal(expected, kintoneDateTime.ToString());
    }

    [Fact]
    public void ParseValidKintoneStringReturnsCorrectValue() {
        var input = "2025-06-06T14:30:00Z";
        var result = KintoneDateTime.Parse(input, KintoneFieldType.DateTime);

        Assert.Equal(new DateTime(2025, 6, 6, 14, 30, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public void TryParseValidStringReturnsTrueAndCorrectValue() {
        var input = "2025-06-06T14:30:00Z";
        var success = KintoneDateTime.TryParse(input, KintoneFieldType.DateTime, out var result);

        Assert.True(success);
        Assert.Equal(new DateTime(2025, 6, 6, 14, 30, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public void TryParseInvalidStringReturnsFalse() {
        var input = "invalid-date-time";
        var success = KintoneDateTime.TryParse(input, KintoneFieldType.DateTime, out var result);

        Assert.False(success);
        Assert.Equal(DateTime.MinValue, result.Value);
    }

    [Fact]
    public void ImplicitConversionFromDateTimeWorksCorrectly() {
        DateTime dt = new DateTime(2025, 6, 6, 14, 30, 0);
        KintoneDateTime kdt = dt;

        Assert.Equal(dt, kdt);
    }

    [Fact]
    public void ImplicitConversionToDateTimeWorksCorrectly() {
        var kdt = new KintoneDateTime(new DateTime(2025, 6, 6, 14, 30, 0));
        DateTime dt = kdt;

        Assert.Equal(new DateTime(2025, 6, 6, 14, 30, 0), dt);
    }
}
