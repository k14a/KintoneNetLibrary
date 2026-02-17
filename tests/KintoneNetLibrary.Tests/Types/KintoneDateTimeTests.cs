using System;
using Xunit;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;

namespace KintoneNetLibrary.Tests.Types;

/// <summary>
/// KintoneDateTimeクラスの単体テストクラス。
/// </summary>
public class KintoneDateTimeTests {
    /// <summary>
    /// 有効なDateTimeを使用してコンストラクタが正しく値を設定することをテストします。
    /// </summary>
    [Fact]
    public void ConstructorWithValidDateTimeSetsValueCorrectly() {
        var dateTime = new DateTime(2025, 6, 6, 14, 30, 0);
        var kintoneDateTime = new KintoneDateTime(dateTime);

        Assert.Equal(dateTime, kintoneDateTime.Value);
    }

    /// <summary>
    /// ToStringメソッドがKintoneの期待されるフォーマットで文字列を返すことをテストします。
    /// </summary>
    [Fact]
    public void ToStringReturnsExpectedKintoneFormat() {
        var dateTime = new DateTime(2025, 6, 6, 14, 30, 0);
        var kintoneDateTime = new KintoneDateTime(dateTime);

        var expected = "2025-06-06T14:30";
        Assert.Equal(expected, kintoneDateTime.ToString());
    }

    /// <summary>
    /// 有効なKintone形式の文字列を解析して正しいDateTime値を返すことをテストします。
    /// </summary>
    [Fact]
    public void ParseValidKintoneStringReturnsCorrectValue() {
        var input = "2025-06-06T14:30:00Z";
        var result = KintoneDateTime.Parse(input, KintoneFieldType.DateTime);

        Assert.Equal(new DateTime(2025, 6, 6, 14, 30, 0, DateTimeKind.Utc), result.Value);
    }

    /// <summary>
    /// 無効な文字列を解析しようとした場合に例外がスローされることをテストします。
    /// </summary>
    [Fact]
    public void TryParseValidStringReturnsTrueAndCorrectValue() {
        var input = "2025-06-06T14:30:00Z";
        var success = KintoneDateTime.TryParse(input, KintoneFieldType.DateTime, out var result);

        Assert.True(success);
        Assert.Equal(new DateTime(2025, 6, 6, 14, 30, 0, DateTimeKind.Utc), result.Value);
    }

    /// <summary>
    /// 無効な文字列を解析しようとした場合にTryParseがfalseを返し、出力値がDateTime.MinValueになることをテストします。
    /// </summary>
    [Fact]
    public void TryParseInvalidStringReturnsFalse() {
        var input = "invalid-date-time";
        var success = KintoneDateTime.TryParse(input, KintoneFieldType.DateTime, out var result);

        Assert.False(success);
        Assert.Equal(DateTime.MinValue, result.Value);
    }

    /// <summary>
    /// DateTimeからKintoneDateTimeへの暗黙的な変換が正しく機能することをテストします。
    /// </summary>
    [Fact]
    public void ImplicitConversionFromDateTimeWorksCorrectly() {
        DateTime dt = new DateTime(2025, 6, 6, 14, 30, 0);
        KintoneDateTime kdt = dt;

        Assert.Equal(dt, kdt);
    }

    /// <summary>
    /// KintoneDateTimeからDateTimeへの暗黙的な変換が正しく機能することをテストします。
    /// </summary>
    [Fact]
    public void ImplicitConversionToDateTimeWorksCorrectly() {
        var kdt = new KintoneDateTime(new DateTime(2025, 6, 6, 14, 30, 0));
        DateTime dt = kdt;

        Assert.Equal(new DateTime(2025, 6, 6, 14, 30, 0), dt);
    }
}
