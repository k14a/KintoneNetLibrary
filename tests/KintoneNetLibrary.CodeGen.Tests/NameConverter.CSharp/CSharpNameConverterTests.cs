using KintoneNetLibrary.CodeGen.Application.Emitters;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.NameConverter.CSharp;

/// <summary>
/// CSharpNameConverter の単体テストクラス。CSharpNameConverter は、kintone のフィールドコードやラベルを C# のプロパティ名に変換するためのクラスであり、その変換ロジックが正しく機能することを確認するためのテストを提供する。
/// </summary>
public class CSharpNameConverterTests {
    private readonly CSharpNameConverter _converter = new();

    // --- システムフィールド ---
    /// <summary>
    /// CSharpNameConverter が kintone のシステムフィールドのラベルを正しい C# のプロパティ名に変換することをテストする。例えば、「作成者」は「Creator」、「更新日時」は「UpdatedTime」など、kintone のシステムフィールドに対応する英語のプロパティ名に変換されることを確認する。
    /// </summary>
    /// <param name="label">kintone のフィールドラベル</param>
    /// <param name="expected">期待される C# のプロパティ名</param>
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
    /// <summary>
    /// CSharpNameConverter が、事前に定義された辞書に基づいて、特定の日本語のフィールドラベルを対応する英語のプロパティ名に変換することをテストする。例えば、「顧客」は「Customer」、「数量」は「Quantity」、「名」は「Name」など、一般的なフィールドラベルが正しい英語のプロパティ名に変換されることを確認する。
    /// </summary>
    /// <param name="label">kintone のフィールドラベル</param>
    /// <param name="expected">期待される C# のプロパティ名</param>
    [Theory]
    [InlineData("顧客", "Customer")]
    [InlineData("数量", "Quantity")]
    [InlineData("名", "Name")]
    public void Convert_JapaneseDictionary_ReturnsMappedEnglish(string label, string expected) {
        var result = this._converter.ToPropertyName(label, expected);
        Assert.Equal(expected, result);
    }

    // --- フィールドコードの安全化 ---
    /// <summary>
    /// CSharpNameConverter が、kintone のフィールドコードを C# のプロパティ名として安全な形式に変換することをテストする。例えば、スペースやハイフンなどの特殊文字を削除し、PascalCase に変換することを確認する。また、数字で始まるコードにはアンダースコアを追加し、C# のキーワードには @ をプレフィックスとして追加することもテストする。
    /// </summary>
    /// <param name="code">kintone のフィールドコード</param>
    /// <param name="expected">期待される C# のプロパティ名</param>
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
    /// <summary>
    /// CSharpNameConverter が、数字で始まるフィールドコードを C# のプロパティ名として安全な形式に変換することをテストする。例えば、「123abc」というコードが「_123abc」というプロパティ名に変換されることを確認する。C# では識別子が数字で始まることはできないため、アンダースコアをプレフィックスとして追加することで、コードが有効な C# のプロパティ名になることをテストする。
    /// </summary>
    [Fact]
    public void Convert_StartsWithNumber_AddsUnderscore() {
        var result = this._converter.ToPropertyName("", "123abc");
        Assert.Equal("_123abc", result);
    }

    // --- C# キーワード回避 ---
    /// <summary>
    /// CSharpNameConverter が、C# のキーワードをフィールドコードとして受け取った場合に、プロパティ名の先頭に @ を追加してキーワード回避することをテストする。例えば、「class」というコードが「@Class」というプロパティ名に変換されることを確認する。これにより、C# のキーワードがプロパティ名として使用される場合でも、コードが有効な C# のプロパティ名になることをテストする。
    /// </summary>
    /// <param name="code">kintone のフィールドコード</param>
    /// <param name="expected">期待される C# のプロパティ名</param>
    [Theory]
    [InlineData("class", "@Class")]
    [InlineData("string", "@String")]
    [InlineData("namespace", "@Namespace")]
    public void Convert_CSharpKeyword_AddsAtPrefix(string code, string expected) {
        var result = this._converter.ToPropertyName("", code);
        Assert.Equal(expected, result);
    }

    // --- ローマ字変換 ---
    /// <summary>
    /// CSharpNameConverter が、特定の日本語のフィールドラベルをローマ字表記に変換することをテストする。例えば、「あ」が「A」、「きゃ」が「Kya」、「っか」が「Kka」、「にゅう」が「NyuU」など、一般的な日本語の文字列が正しいローマ字表記のプロパティ名に変換されることを確認する。これにより、日本語のフィールドラベルが英語のプロパティ名として適切に変換されることをテストする。
    /// </summary>
    /// <param name="label">kintone のフィールドラベル</param>
    /// <param name="expected">期待される C# のプロパティ名</param>
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
    /// <summary>
    /// CSharpNameConverter が、フィールドラベルがすでに ASCII 文字で構成されている場合に、PascalCase に変換することをテストする。例えば、「customer_name」というラベルが「CustomerName」というプロパティ名に変換されることを確認する。これにより、すでに英語のフィールドラベルが適切な C# のプロパティ名に変換されることをテストする。
    /// </summary>
    /// <param name="label">kintone のフィールドラベル</param>
    /// <param name="expected">期待される C# のプロパティ名</param>
    [Theory]
    [InlineData("customer_name", "CustomerName")]
    [InlineData("orderDate", "OrderDate")]
    public void Convert_AsciiLabel_UsesPascalCase(string label, string expected) {
        var result = this._converter.ToPropertyName(label, expected);
        Assert.Equal(expected, result);
    }

    // --- fallback ---
    /// <summary>
    /// CSharpNameConverter が、フィールドラベルもフィールドコードも特定の変換ルールにマッチしない場合に、PascalCase に変換することをテストする。例えば、「未知のラベル」というフィールドラベルと「unknown_code」というフィールドコードが与えられた場合に、「UnknownCode」というプロパティ名に変換されることを確認する。これにより、特定の変換ルールにマッチしない場合でも、コードが有効な C# のプロパティ名になることをテストする。
    /// </summary>
    [Fact]
    public void Convert_Fallback_ReturnsPascalCaseCode() {
        var result = this._converter.ToPropertyName("未知のラベル", "unknown_code");
        Assert.Equal("UnknownCode", result);
    }
}
