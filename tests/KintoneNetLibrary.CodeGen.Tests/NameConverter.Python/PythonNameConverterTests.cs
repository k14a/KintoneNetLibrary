using KintoneNetLibrary.CodeGen.Application.Emitters;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.NameConverter.Python;

public class PythonNameConverterTests {
    private readonly PythonNameConverter _converter = new();

    // -----------------------------
    // SelectBaseName
    // -----------------------------
    [Fact]
    public void SelectBaseName_CodeHasPriority() {
        var result = this._converter.ToPropertyName("顧客名", "customer_name");
        Assert.Equal("customer_name", result);
    }

    // -----------------------------
    // ToClassName
    // -----------------------------
    [Fact]
    public void ToClassName_ConvertsToPascalCase() {
        var result = this._converter.ToClassName("顧客", "customer");
        Assert.Equal("Customer", result);
    }

    [Fact]
    public void ToClassName_RomanizesJapanese() {
        var result = this._converter.ToClassName("明細", "明細");
        // "明細" → meisai → Meisai
        Assert.Equal("Meisai", result);
    }

    [Fact]
    public void ToClassName_UsesFallbackWhenRomanizationFails() {
        var result = this._converter.ToClassName("", "");
        Assert.Equal("App", result[..3]); // App{code}
    }

    [Fact]
    public void ToClassName_SanitizesInvalidCharacters() {
        var result = this._converter.ToClassName("テスト", "test-name");
        Assert.Equal("TestName", result);
    }

    // -----------------------------
    // ToPropertyName
    // -----------------------------
    [Theory]
    [InlineData("CreatedTime", "created_time")]
    [InlineData("UpdatedTime", "updated_time")]
    [InlineData("Creator", "creator")]
    [InlineData("Modifier", "modifier")]
    public void ToPropertyName_SystemFields_AreFixed(string code, string expected) {
        var result = this._converter.ToPropertyName("", code);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToPropertyName_AsciiCode_UsesSnakeCase() {
        var result = this._converter.ToPropertyName("顧客名", "customerName");
        Assert.Equal("customer_name", result);
    }

    [Fact]
    public void ToPropertyName_Japanese_RomanizesAndSnakeCases() {
        var result = this._converter.ToPropertyName("数量", "数量");
        Assert.Equal("quantity", result);
    }

    [Fact]
    public void ToPropertyName_InvalidRomanization_ReturnsInvalid() {
        var result = this._converter.ToPropertyName("", "");
        Assert.Equal("INVALID_FIELD_NAME", result);
    }

    // -----------------------------
    // Sanitize
    // -----------------------------
    [Fact]
    public void ToPropertyName_SanitizesSpecialCharacters() {
        var result = this._converter.ToPropertyName("テスト", "test!@#name");
        Assert.Equal("test_name", result);
    }

    [Fact]
    public void ToPropertyName_CollapsesMultipleUnderscores() {
        var result = this._converter.ToPropertyName("テスト", "test___name");
        Assert.Equal("test_name", result);
    }

    [Fact]
    public void ToPropertyName_TrimsUnderscores() {
        var result = this._converter.ToPropertyName("テスト", "__test__name__");
        Assert.Equal("test_name", result);
    }
}
