using System;
using System.Collections.Generic;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;
using Xunit;
using FluentAssertions;
using KintoneNetLibrary.Domain.Access;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

/// <summary>
/// KintoneQueryValidatorのテストクラス。
/// </summary>
public class KintoneQueryValidatorTests {
    /// <summary>
    /// ValidateLikeClauseメソッドが英数字のみの文字列に対して警告を出すことをテストします。
    /// </summary>
    private class SampleModel : KintoneModelBase<SampleModel> {
        public override int AppID { get; init; } = 0;
        public override KintoneAccessBase Access { get; init; } = new ApiTokenAccess("dummyDomain", "dummyApiToken");

        [KintoneItem(fieldCode: "UUID")]
        public string Uuid { get; set; } = string.Empty;

        [KintoneItem(fieldCode: "Title")]
        public string Title { get; set; } = string.Empty;

        [KintoneItem(fieldCode: "MultiSelector")]
        public IEnumerable<string> MultiSelector { get; set; } = [];
    }

    /// <summary>
    /// ValidateLikeClauseメソッドが英数字のみの文字列に対して警告を出すことをテストします。
    /// </summary>
    [Fact]
    public void ValidateLikeClauseShouldWarnOnAlphaNumericOnly() {
        // Arrange
        var warnings = new List<string>();
        var query = @"Title like ""abc123"" and UUID = ""xyz""";

        // Act
        KintoneQueryValidator.ValidateLikeClause(query, warnings.Add);

        // Assert
        warnings.Should().ContainSingle();
        warnings[0].Should().Contain("abc123");
    }

    /// <summary>
    /// ValidateLikeClauseメソッドが日本語や記号を含む文字列に対して警告を出さないことをテストします。
    /// </summary>
    [Fact]
    public void ValidateLikeClauseShouldNotWarnOnJapaneseOrSymbols() {
        // Arrange
        var warnings = new List<string>();
        var query = @"Title like ""タイトル123"" or Title like ""abc_123""";

        // Act
        KintoneQueryValidator.ValidateLikeClause(query, warnings.Add);

        // Assert
        warnings.Should().BeEmpty();
    }

    /// <summary>
    /// ValidateFieldCodesメソッドが存在しないフィールドコードを含むクエリに対して例外をスローすることをテストします。
    /// </summary>
    [Fact]
    public void ValidateFieldCodesShouldThrowOnInvalidField() {
        // Arrange
        var query = @"Uuid = ""abc"" and InvalidField = ""123""";

        // Act
        var act = () => KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*InvalidField*");
    }

    /// <summary>
    /// ValidateFieldCodesメソッドが存在しないフィールドコードを含むクエリに対して、throwOnErrorがfalseの場合は例外をスローせず、警告を出すことをテストします。
    /// </summary>
    [Fact]
    public void ValidateFieldCodesShouldWarnOnInvalidFieldWhenThrowOnErrorFalse() {
        // Arrange
        var warnings = new List<string>();
        var query = @"Title = ""abc"" and NGField = ""xxx""";

        // Act
        KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query, throwOnError: false, onWarn: warnings.Add);

        // Assert
        warnings.Should().ContainSingle()
            .Which.Should().Contain("NGField");
    }

    /// <summary>
    /// ValidateFieldCodesメソッドが存在するフィールドコードのみを含むクエリに対して、警告も例外も出さないことをテストします。
    /// </summary>
    [Fact]
    public void ValidateFieldCodesShouldNotWarnOrThrowWhenFieldCodesAreValid() {
        // Arrange
        var query = @"UUID = ""abc"" and Title = ""xyz""";

        // Act
        var act = () => KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// ValidateFieldCodesメソッドがリテラルやキーワードを無視することをテストします。
    /// </summary>
    [Fact]
    public void ValidateFieldCodesShouldIgnoreLiteralsAndKeywords() {
        // Arrange
        var query = @"UUID = ""abc"" and 100 > 10 order by Title desc";

        // Act
        var act = () => KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query);

        // Assert
        act.Should().NotThrow();
    }
}
