using System;
using Xunit;
using KintoneNetLibrary.Domain.Entities;
using System.Globalization;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Tests.Types;

/// <summary>
/// KintoneTimeOnlyクラスの単体テストクラス。
/// </summary>
public class KintoneTimeOnlyTests {
    /// <summary>
    /// 有効なTimeOnlyを使用してコンストラクタが正しく値を設定することをテストします。
    /// </summary>
    [Fact]
    public void ConstructorWithValidTimeOnlySetsValueCorrectly() {
        var time = new TimeOnly(14, 30);
        var kto = new KintoneTimeOnly(time);

        Assert.Equal(time, kto.Value);
    }

    /// <summary>
    /// 有効な文字列を使用してコンストラクタが正しく値を設定することをテストします。
    /// </summary>
    [Fact]
    public void ConstructorWithValidStringSetsValueCorrectly() {
        var raw = "14:30";
        var kto = new KintoneTimeOnly(raw, KintoneFieldType.Time);

        Assert.Equal(new TimeOnly(14, 30), kto.Value);
    }

    /// <summary>
    /// 無効な文字列を使用してコンストラクタがnull値を設定することをテストします。
    /// </summary>
    [Fact]
    public void ConstructorWithInvalidStringSetsNullValue() {
        var kto = new KintoneTimeOnly("invalid", KintoneFieldType.Time);

        Assert.Null(kto.Value);
    }

    /// <summary>
    /// ToStringメソッドがKintoneの期待されるフォーマットで文字列を返すことをテストします。
    /// </summary>
    [Fact]
    public void ToStringReturnsExpectedFormat() {
        var kto = new KintoneTimeOnly(new TimeOnly(8, 5));

        Assert.Equal("08:05", kto.ToString());
    }

    /// <summary>
    /// TimeOnlyからKintoneTimeOnlyへの暗黙的な変換が正しく機能することをテストします。
    /// </summary>
    [Fact]
    public void ImplicitConversionFromTimeOnlyWorksCorrectly() {
        TimeOnly time = new(18, 45);
        KintoneTimeOnly kto = time;

        Assert.Equal(time, kto.Value);
    }

    /// <summary>
    /// KintoneTimeOnlyからTimeOnlyへの暗黙的な変換が正しく機能することをテストします。
    /// </summary>
    [Fact]
    public void ImplicitConversionToTimeOnlyWorksCorrectly() {
        var kto = new KintoneTimeOnly(new TimeOnly(6, 0));
        TimeOnly time = kto;

        Assert.Equal(new TimeOnly(6, 0), time);
    }

    /// <summary>
    /// TimeSpanからKintoneTimeOnlyへの明示的な変換が正しく機能することをテストします。
    /// </summary>
    [Fact]
    public void ExplicitConversionFromTimeSpanWorksCorrectly() {
        var span = new TimeSpan(15, 0, 0);
        var kto = (KintoneTimeOnly)span;

        Assert.Equal(new TimeOnly(15, 0), kto.Value);
    }

    /// <summary>
    /// 無効なTimeSpanからKintoneTimeOnlyへの明示的な変換が例外をスローすることをテストします。
    /// </summary>
    [Fact]
    public void ExplicitConversionFromInvalidTimeSpanThrows() {
        var span = new TimeSpan(25, 0, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => (KintoneTimeOnly)span);
    }

    /// <summary>
    /// ToJsonメソッドが値を正しいフォーマットで返すことをテストします。
    /// </summary>
    [Fact]
    public void ToJsonWithValueReturnsFormattedTime() {
        var kto = new KintoneTimeOnly(new TimeOnly(23, 59));

        Assert.Equal("23:59", kto.ToJson());
    }

    /// <summary>
    /// ToJsonメソッドが値がnullの場合にnullを返すことをテストします。
    /// </summary>
    [Fact]
    public void ToJsonWithoutValueReturnsNull() {
        var kto = new KintoneTimeOnly(null);

        Assert.Null(kto.ToJson());
    }

    /// <summary>
    /// 有効なKintone形式の文字列を解析して正しいTimeOnly値を返すことをテストします。
    /// </summary>
    [Fact]
    public void ParseValidStringReturnsCorrectValue() {
        var kto = KintoneTimeOnly.Parse("14:15");

        Assert.Equal(new TimeOnly(14, 15), kto.Value);
    }

    /// <summary>
    /// 無効な文字列を解析しようとした場合に例外がスローされることをテストします。
    /// </summary>
    [Fact]
    public void TryParseValidStringReturnsTrueAndCorrectValue() {
        var success = KintoneTimeOnly.TryParse("07:45", out var kto);

        Assert.True(success);
        Assert.Equal(new TimeOnly(7, 45), kto.Value);
    }

    /// <summary>
    /// 無効な文字列を解析しようとした場合にTryParseがfalseを返し、出力値がTimeOnly.MinValueになることをテストします。
    /// </summary>
    [Fact]
    public void TryParseInvalidStringReturnsFalse() {
        var success = KintoneTimeOnly.TryParse("not-a-time", out var kto);

        Assert.False(success);
        Assert.Equal(TimeOnly.MinValue, kto.Value);
    }
}
