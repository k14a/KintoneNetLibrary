using System;
using Xunit;
using KintoneNetLibrary.Domain.Entities;
using System.Globalization;

namespace KintoneNetLibrary.Tests.Types;

public class KintoneTimeOnlyTests {
    [Fact]
    public void Constructor_WithValidTimeOnly_SetsValueCorrectly() {
        var time = new TimeOnly(14, 30);
        var kto = new KintoneTimeOnly(time);

        Assert.Equal(time, kto.Value);
    }

    [Fact]
    public void Constructor_WithValidString_SetsValueCorrectly() {
        var raw = "14:30";
        var kto = new KintoneTimeOnly(raw, KintoneFieldType.Time);

        Assert.Equal(new TimeOnly(14, 30), kto.Value);
    }

    [Fact]
    public void Constructor_WithInvalidString_SetsNullValue() {
        var kto = new KintoneTimeOnly("invalid", KintoneFieldType.Time);

        Assert.Null(kto.Value);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat() {
        var kto = new KintoneTimeOnly(new TimeOnly(8, 5));

        Assert.Equal("08:05", kto.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromTimeOnly_WorksCorrectly() {
        TimeOnly time = new(18, 45);
        KintoneTimeOnly kto = time;

        Assert.Equal(time, kto.Value);
    }

    [Fact]
    public void ImplicitConversion_ToTimeOnly_WorksCorrectly() {
        var kto = new KintoneTimeOnly(new TimeOnly(6, 0));
        TimeOnly time = kto;

        Assert.Equal(new TimeOnly(6, 0), time);
    }

    [Fact]
    public void ExplicitConversion_FromTimeSpan_WorksCorrectly() {
        var span = new TimeSpan(15, 0, 0);
        var kto = (KintoneTimeOnly)span;

        Assert.Equal(new TimeOnly(15, 0), kto.Value);
    }

    [Fact]
    public void ExplicitConversion_FromInvalidTimeSpan_Throws() {
        var span = new TimeSpan(25, 0, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => (KintoneTimeOnly)span);
    }

    [Fact]
    public void ToJson_WithValue_ReturnsFormattedTime() {
        var kto = new KintoneTimeOnly(new TimeOnly(23, 59));

        Assert.Equal("23:59", kto.ToJson());
    }

    [Fact]
    public void ToJson_WithoutValue_ReturnsNull() {
        var kto = new KintoneTimeOnly(null);

        Assert.Null(kto.ToJson());
    }

    [Fact]
    public void Parse_ValidString_ReturnsCorrectValue() {
        var kto = KintoneTimeOnly.Parse("14:15");

        Assert.Equal(new TimeOnly(14, 15), kto.Value);
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrueAndCorrectValue() {
        var success = KintoneTimeOnly.TryParse("07:45", out var kto);

        Assert.True(success);
        Assert.Equal(new TimeOnly(7, 45), kto.Value);
    }

    [Fact]
    public void TryParse_InvalidString_ReturnsFalse() {
        var success = KintoneTimeOnly.TryParse("not-a-time", out var kto);

        Assert.False(success);
        Assert.Equal(TimeOnly.MinValue, kto.Value);
    }
}
