using System;
using System.Collections.Generic;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Helpers;
using Xunit;
using FluentAssertions;

namespace KintoneNetLibrary.Tests.Helpers.Queries;

public class KintoneQueryValidatorTests {
    private class SampleModel : KintoneModelBase {
        public override int AppID => TestEnv.Settings.AppID;

        [KintoneItem(fieldCode: "UUID")]
        public string Uuid { get; set; } = string.Empty;

        [KintoneItem(fieldCode: "Title")]
        public string Title { get; set; } = string.Empty;

        [KintoneItem(fieldCode: "MultiSelector")]
        public IEnumerable<string> MultiSelector { get; set; } = [];
    }

    [Fact]
    public void ValidateLikeClause_ShouldWarnOnAlphaNumericOnly() {
        // Arrange
        var warnings = new List<string>();
        var query = @"Title like ""abc123"" and UUID = ""xyz""";

        // Act
        KintoneQueryValidator.ValidateLikeClause(query, warnings.Add);

        // Assert
        warnings.Should().ContainSingle();
        warnings[0].Should().Contain("abc123");
    }
    [Fact]
    public void ValidateLikeClause_ShouldNotWarnOnJapaneseOrSymbols() {
        // Arrange
        var warnings = new List<string>();
        var query = @"Title like ""タイトル123"" or Title like ""abc_123""";

        // Act
        KintoneQueryValidator.ValidateLikeClause(query, warnings.Add);

        // Assert
        warnings.Should().BeEmpty();
    }
    [Fact]
    public void ValidateFieldCodes_ShouldThrowOnInvalidField() {
        // Arrange
        var query = @"Uuid = ""abc"" and InvalidField = ""123""";

        // Act
        var act = () => KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*InvalidField*");
    }
    [Fact]
    public void ValidateFieldCodes_ShouldWarnOnInvalidField_WhenThrowOnErrorFalse() {
        // Arrange
        var warnings = new List<string>();
        var query = @"Title = ""abc"" and NGField = ""xxx""";

        // Act
        KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query, throwOnError: false, onWarn: warnings.Add);

        // Assert
        warnings.Should().ContainSingle()
            .Which.Should().Contain("NGField");
    }
    [Fact]
    public void ValidateFieldCodes_ShouldNotWarnOrThrow_WhenFieldCodesAreValid() {
        // Arrange
        var query = @"UUID = ""abc"" and Title = ""xyz""";

        // Act
        var act = () => KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query);

        // Assert
        act.Should().NotThrow();
    }
    [Fact]
    public void ValidateFieldCodes_ShouldIgnoreLiteralsAndKeywords() {
        // Arrange
        var query = @"UUID = ""abc"" and 100 > 10 order by Title desc";

        // Act
        var act = () => KintoneQueryValidator.ValidateFieldCodes<SampleModel>(query);

        // Assert
        act.Should().NotThrow();
    }
}
