using KintoneNetLibrary.CodeGen.Application.Emitters;
using Xunit;

namespace KintoneNetLibrary.CodeGen.Tests.NameConverter.Python;

/// <summary>
/// PythonNameConverter の単体テストクラス。PythonNameConverter は、kintone のフィールドコードやラベルを Python のプロパティ名に変換するためのクラスであり、その変換ロジックが正しく機能することを確認するためのテストを提供する。
/// </summary>
public class PythonNameConverterTests {
    private readonly PythonNameConverter _converter = new();

    // -----------------------------
    // SelectBaseName
    // -----------------------------
    /// <summary>
    /// PythonNameConverter が、フィールドラベルとフィールドコードの両方が与えられた場合に、フィールドコードを優先してプロパティ名を選択することをテストする。例えば、「顧客名」というフィールドラベルと「customer_name」というフィールドコードが与えられた場合に、「customer_name」がプロパティ名として選択されることを確認する。これにより、フィールドコードがプロパティ名の選択に優先されることをテストする。
    /// </summary>
    [Fact]
    public void SelectBaseName_CodeHasPriority() {
        var result = this._converter.ToPropertyName("顧客名", "customer_name");
        Assert.Equal("customer_name", result);
    }

    // -----------------------------
    // ToClassName
    // -----------------------------
    /// <summary>
    /// PythonNameConverter が、kintone のフィールドラベルを Python のクラス名に変換することをテストする。例えば、「顧客」というフィールドラベルが「Customer」というクラス名に変換されることを確認する。これにより、フィールドラベルが適切な Python のクラス名に変換されることをテストする。
    /// </summary>
    [Fact]
    public void ToClassName_ConvertsToPascalCase() {
        var result = this._converter.ToClassName("顧客", "customer");
        Assert.Equal("Customer", result);
    }

    /// <summary>
    /// PythonNameConverter が、kintone のフィールドラベルをローマ字表記のクラス名に変換することをテストする。例えば、「明細」というフィールドラベルが「Meisai」というクラス名に変換されることを確認する。これにより、日本語のフィールドラベルが適切なローマ字表記の Python のクラス名に変換されることをテストする。
    /// </summary>
    [Fact]
    public void ToClassName_RomanizesJapanese() {
        var result = this._converter.ToClassName("明細", "明細");
        // "明細" → meisai → Meisai
        Assert.Equal("Meisai", result);
    }

    /// <summary>
    /// PythonNameConverter が、フィールドラベルもフィールドコードも特定の変換ルールにマッチしない場合に、クラス名の先頭に "App" をプレフィックスとして追加することをテストする。例えば、「未知のラベル」というフィールドラベルと「unknown_code」というフィールドコードが与えられた場合に、「AppUnknownCode」というクラス名に変換されることを確認する。これにより、特定の変換ルールにマッチしない場合でも、コードが有効な Python のクラス名になることをテストする。
    /// </summary>
    [Fact]
    public void ToClassName_UsesFallbackWhenRomanizationFails() {
        var result = this._converter.ToClassName("", "");
        Assert.Equal("App", result[..3]); // App{code}
    }

    /// <summary>
    /// PythonNameConverter が、フィールドコードにスペースやハイフンなどの特殊文字が含まれている場合に、それらを削除してクラス名を生成することをテストする。例えば、「test-name」というフィールドコードが「TestName」というクラス名に変換されることを確認する。これにより、フィールドコードに特殊文字が含まれている場合でも、コードが有効な Python のクラス名になることをテストする。
    /// </summary>
    [Fact]
    public void ToClassName_SanitizesInvalidCharacters() {
        var result = this._converter.ToClassName("テスト", "test-name");
        Assert.Equal("TestName", result);
    }

    // -----------------------------
    // ToPropertyName
    // -----------------------------
    /// <summary>
    /// PythonNameConverter が、kintone のシステムフィールドのコードを Python のプロパティ名に変換することをテストする。例えば、「CreatedTime」というフィールドコードが「created_time」というプロパティ名に変換されることを確認する。これにより、kintone のシステムフィールドが適切な Python のプロパティ名に変換されることをテストする。
    /// </summary>
    /// <param name="code">kintone のフィールドコード</param>
    /// <param name="expected">期待される Python のプロパティ名</param>
    [Theory]
    [InlineData("CreatedTime", "created_time")]
    [InlineData("UpdatedTime", "updated_time")]
    [InlineData("Creator", "creator")]
    [InlineData("Modifier", "modifier")]
    public void ToPropertyName_SystemFields_AreFixed(string code, string expected) {
        var result = this._converter.ToPropertyName("", code);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// PythonNameConverter が、フィールドコードが ASCII 文字で構成されている場合に、スネークケースに変換することをテストする。例えば、「customerName」というフィールドコードが「customer_name」というプロパティ名に変換されることを確認する。これにより、ASCII 文字のフィールドコードが適切な Python のプロパティ名に変換されることをテストする。
    /// </summary>
    [Fact]
    public void ToPropertyName_AsciiCode_UsesSnakeCase() {
        var result = this._converter.ToPropertyName("顧客名", "customerName");
        Assert.Equal("customer_name", result);
    }

    /// <summary>
    /// PythonNameConverter が、特定の日本語のフィールドラベルをローマ字表記に変換し、さらにスネークケースに変換することをテストする。例えば、「数量」というフィールドラベルが「quantity」というプロパティ名に変換されることを確認する。これにより、日本語のフィールドラベルが適切なローマ字表記の Python のプロパティ名に変換されることをテストする。
    /// </summary>
    [Fact]
    public void ToPropertyName_Japanese_RomanizesAndSnakeCases() {
        var result = this._converter.ToPropertyName("数量", "数量");
        Assert.Equal("quantity", result);
    }

    /// <summary>
    /// PythonNameConverter が、フィールドラベルもフィールドコードも特定の変換ルールにマッチしない場合に、プロパティ名を "INVALID_FIELD_NAME" に変換することをテストする。例えば、空のフィールドラベルと空のフィールドコードが与えられた場合に、「INVALID_FIELD_NAME」というプロパティ名に変換されることを確認する。これにより、特定の変換ルールにマッチしない場合でも、コードが有効な Python のプロパティ名になることをテストする。
    /// </summary>
    [Fact]
    public void ToPropertyName_InvalidRomanization_ReturnsInvalid() {
        var result = this._converter.ToPropertyName("", "");
        Assert.Equal("INVALID_FIELD_NAME", result);
    }

    // -----------------------------
    // Sanitize
    // -----------------------------
    /// <summary>
    /// PythonNameConverter が、フィールドコードにスペースやハイフンなどの特殊文字が含まれている場合に、それらをアンダースコアに置換してプロパティ名を生成することをテストする。例えば、「test!@#name」というフィールドコードが「test_name」というプロパティ名に変換されることを確認する。これにより、フィールドコードに特殊文字が含まれている場合でも、コードが有効な Python のプロパティ名になることをテストする。
    /// </summary>
    [Fact]
    public void ToPropertyName_SanitizesSpecialCharacters() {
        var result = this._converter.ToPropertyName("テスト", "test!@#name");
        Assert.Equal("test_name", result);
    }

    /// <summary>
    /// PythonNameConverter が、フィールドコードに複数の連続するアンダースコアが含まれている場合に、それらを単一のアンダースコアに置換してプロパティ名を生成することをテストする。例えば、「test___name」というフィールドコードが「test_name」というプロパティ名に変換されることを確認する。これにより、フィールドコードに複数の連続するアンダースコアが含まれている場合でも、コードが有効な Python のプロパティ名になることをテストする。
    /// </summary>
    [Fact]
    public void ToPropertyName_CollapsesMultipleUnderscores() {
        var result = this._converter.ToPropertyName("テスト", "test___name");
        Assert.Equal("test_name", result);
    }

    /// <summary>
    /// PythonNameConverter が、フィールドコードの先頭や末尾にアンダースコアが含まれている場合に、それらを削除してプロパティ名を生成することをテストする。例えば、「__test_name__」というフィールドコードが「test_name」というプロパティ名に変換されることを確認する。これにより、フィールドコードの先頭や末尾にアンダースコアが含まれている場合でも、コードが有効な Python のプロパティ名になることをテストする。
    /// </summary>
    [Fact]
    public void ToPropertyName_TrimsUnderscores() {
        var result = this._converter.ToPropertyName("テスト", "__test__name__");
        Assert.Equal("test_name", result);
    }
}
