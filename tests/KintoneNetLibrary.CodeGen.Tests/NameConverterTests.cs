using KintoneNetLibrary.CodeGen.Application.Emitters;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests;

public class NameConverterTests {
    private readonly NameConverter _converter = new();

    [Fact]
    public void ToPropertyNameJapaneseLabelUsesDictionary() {
        var result = this._converter.ToPropertyName("顧客名", "customer_name");
        Assert.Equal("CustomerName", result);
    }

    [Fact]
    public void ToPropertyNameJapaneseLabelRomanFallback() {
        var result = this._converter.ToPropertyName("あいうえお", "aiueo");
        Assert.Equal("Aiueo", result);
    }

    [Fact]
    public void ToPropertyNameEnglishLabelPascalCase() {
        var result = this._converter.ToPropertyName("email address", "email_address");
        Assert.Equal("EmailAddress", result);
    }

    [Fact]
    public void ToPropertyNameLabelEmptyUsesCodeFallback() {
        var result = this._converter.ToPropertyName("", "customer_name");
        Assert.Equal("CustomerName", result);
    }

    [Fact]
    public void ToPropertyNameCSharpKeywordIsEscaped() {
        var result = this._converter.ToPropertyName("class", "class");
        Assert.Equal("@Class", result);
    }

    [Fact]
    public void ToPropertyNameStartsWithNumberIsPrefixed() {
        var result = this._converter.ToPropertyName("123abc", "123abc");
        Assert.Equal("_123abc", result);
    }
}
