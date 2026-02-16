using KintoneNetLibrary.CodeGen.Application.Emitters;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.NameConverter.CSharp;

public class CSharpNameConverterTests {
    private readonly CSharpNameConverter _converter = new();

    // --- システムフィールド ---
    [Theory]
    [InlineData("作成者", "Creator")]
    [InlineData("更新者", "Modifier")]
    [InlineData("作成日時", "CreatedTime")]
    [InlineData("更新日時", "UpdatedTime")]
    [InlineData("ステータス", "Status")]
    public void Convert_SystemFields_ReturnsMappedName(string label, string expected) {
        var result = this._converter.ToPropertyName(label, label);
        Assert.Equal(expected, result);
    }

    // --- 辞書変換 ---
    [Theory]
    [InlineData("顧客", "Customer")]
    [InlineData("数量", "Quantity")]
    [InlineData("名", "Name")]
    public void Convert_JapaneseDictionary_ReturnsMappedEnglish(string label, string expected) {
        var result = this._converter.ToPropertyName(label, expected);
        Assert.Equal(expected, result);
    }

    // --- フィールドコードの安全化 ---
    [Theory]
    [InlineData("customer-name", "CustomerName")]
    [InlineData("order id", "OrderId")]
    [InlineData("___abc___", "Abc")]
    [InlineData("!@#abc", "Abc")]
    public void Convert_SanitizeFieldCode_ProducesSafeIdentifier(string code, string expected) {
        var result = this._converter.ToPropertyName("", code);
        Assert.Equal(expected, result);
    }

    // --- 数字始まり ---
    [Fact]
    public void Convert_StartsWithNumber_AddsUnderscore() {
        var result = this._converter.ToPropertyName("", "123abc");
        Assert.Equal("_123abc", result);
    }

    // --- C# キーワード回避 ---
    [Theory]
    [InlineData("class", "@Class")]
    [InlineData("string", "@String")]
    [InlineData("namespace", "@Namespace")]
    public void Convert_CSharpKeyword_AddsAtPrefix(string code, string expected) {
        var result = this._converter.ToPropertyName("", code);
        Assert.Equal(expected, result);
    }

    // --- ローマ字変換 ---
    [Theory]
    [InlineData("あ", "A")]
    [InlineData("きゃ", "Kya")]
    [InlineData("っか", "Kka")]
    [InlineData("にゅう", "NyuU")]
    public void Convert_Romanization_Works(string label, string expected) {
        var result = this._converter.ToPropertyName(label, expected);
        Assert.Equal(expected, result);
    }

    // --- ラベルが ASCII の場合 ---
    [Theory]
    [InlineData("customer_name", "CustomerName")]
    [InlineData("orderDate", "OrderDate")]
    public void Convert_AsciiLabel_UsesPascalCase(string label, string expected) {
        var result = this._converter.ToPropertyName(label, expected);
        Assert.Equal(expected, result);
    }

    // --- fallback ---
    [Fact]
    public void Convert_Fallback_ReturnsPascalCaseCode() {
        var result = this._converter.ToPropertyName("未知のラベル", "unknown_code");
        Assert.Equal("UnknownCode", result);
    }
}
