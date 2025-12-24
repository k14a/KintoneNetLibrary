using System;
using Xunit;
using KintoneNetLibrary.Domain.Entities;
using System.Globalization;

namespace KintoneNetLibrary.Tests.Types;

public class KintoneTimeOnlyTests {
    [Fact]
    public void ConstructorWithValidTimeOnlySetsValueCorrectly() {
        var time = new TimeOnly(14, 30);
        var kto = new KintoneTimeOnly(time);

        Assert.Equal(time, kto.Value);
    }

    [Fact]
    public void ConstructorWithValidStringSetsValueCorrectly() {
        var raw = "14:30";
        var kto = new KintoneTimeOnly(raw, KintoneFieldType.Time);

        Assert.Equal(new TimeOnly(14, 30), kto.Value);
    }

    [Fact]
    public void ConstructorWithInvalidStringSetsNullValue() {
        var kto = new KintoneTimeOnly("invalid", KintoneFieldType.Time);

        Assert.Null(kto.Value);
    }

    [Fact]
    public void ToStringReturnsExpectedFormat() {
        var kto = new KintoneTimeOnly(new TimeOnly(8, 5));

        Assert.Equal("08:05", kto.ToString());
    }

    [Fact]
    public void ImplicitConversionFromTimeOnlyWorksCorrectly() {
        TimeOnly time = new(18, 45);
        KintoneTimeOnly kto = time;

        Assert.Equal(time, kto.Value);
    }

    [Fact]
    public void ImplicitConversionToTimeOnlyWorksCorrectly() {
        var kto = new KintoneTimeOnly(new TimeOnly(6, 0));
        TimeOnly time = kto;

        Assert.Equal(new TimeOnly(6, 0), time);
    }

    [Fact]
    public void ExplicitConversionFromTimeSpanWorksCorrectly() {
        var span = new TimeSpan(15, 0, 0);
        var kto = (KintoneTimeOnly)span;

        Assert.Equal(new TimeOnly(15, 0), kto.Value);
    }

    [Fact]
    public void ExplicitConversionFromInvalidTimeSpanThrows() {
        var span = new TimeSpan(25, 0, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => (KintoneTimeOnly)span);
    }

    [Fact]
    public void ToJsonWithValueReturnsFormattedTime() {
        var kto = new KintoneTimeOnly(new TimeOnly(23, 59));

        Assert.Equal("23:59", kto.ToJson());
    }

    [Fact]
    public void ToJsonWithoutValueReturnsNull() {
        var kto = new KintoneTimeOnly(null);

        Assert.Null(kto.ToJson());
    }

    [Fact]
    public void ParseValidStringReturnsCorrectValue() {
        var kto = KintoneTimeOnly.Parse("14:15");

        Assert.Equal(new TimeOnly(14, 15), kto.Value);
    }

    [Fact]
    public void TryParseValidStringReturnsTrueAndCorrectValue() {
        var success = KintoneTimeOnly.TryParse("07:45", out var kto);

        Assert.True(success);
        Assert.Equal(new TimeOnly(7, 45), kto.Value);
    }

    [Fact]
    public void TryParseInvalidStringReturnsFalse() {
        var success = KintoneTimeOnly.TryParse("not-a-time", out var kto);

        Assert.False(success);
        Assert.Equal(TimeOnly.MinValue, kto.Value);
    }
}
