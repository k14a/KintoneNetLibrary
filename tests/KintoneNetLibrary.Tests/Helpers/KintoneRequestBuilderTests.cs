using System.Reflection;
using KintoneNetLibrary.Infrastructure.Helpers;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// KintoneRequestBuilder クラスのユニットテスト。
/// </summary>
public class KintoneRequestBuilderTests {
    #region <<Test methods>>
    /// <summary>
    /// クエリやフィールドが指定されない場合、URIにアプリIdのみが含まれることを確認するテスト。
    /// </summary>
    [Fact]
    public void BuildFindRequestUriWithoutQueryOrFieldsReturnsAppOnly() {
        var uri = KintoneRequestBuilder.BuildFindRequestUri(new Uri("https://example.cybozu.com"), "/v1/records.json", 123);
        Assert.Equal("https://example.cybozu.com/v1/records.json?app=123", uri.ToString());
    }

    /// <summary>
    /// クエリとフィールドが指定された場合、URIに正しくエンコードされたクエリとフィールドが含まれることを確認するテスト。
    /// </summary>
    [Fact]
    public void BuildFindRequestUriWithQueryAndFieldsReturnsFullUri() {
        var uri = KintoneRequestBuilder.BuildFindRequestUri(
            new Uri("https://foo.cybozu.com"),
            "/v1/records.json",
            456,
            "status=\"処理中\"",
            ["$id", "status"]);

        var queryText = Uri.EscapeDataString("status=\"処理中\"");
        var field0 = Uri.EscapeDataString("$id");
        var field1 = Uri.EscapeDataString("status");
        var expected = $"app=456&query={queryText}&fields[0]={field0}&fields[1]={field1}";
        Assert.Contains(expected, uri.Query);
    }

    /// <summary>
    /// EnsureMinimumFields メソッドが、null または空のリストが渡された場合に null を返すことを確認するテスト。
    /// </summary>
    /// <param name="input"></param>
    /// <param name="expected"></param>
#pragma warning disable xUnit1012, xUnit1026
    [Theory]
    [InlineData(null, null)]
    [InlineData(new string[] { }, null)]
    public void EnsureMinimumFieldsReturnsNullWhenInputIsNullOrEmpty(string[] input, string[] expected) {
        var result = InvokeEnsureMinimumFields(input);
        Assert.Null(result);
    }
#pragma warning restore xUnit1012, xUnit1026

    /// <summary>
    /// EnsureMinimumFields メソッドが、必須フィールドが存在しない場合にそれらを追加することを確認するテスト。
    /// </summary>
    [Fact]
    public void EnsureMinimumFieldsAddsRequiredFieldsWhenMissing() {
        var input = new List<string> { "name", "email" };
        var result = InvokeEnsureMinimumFields(input);
        var expected = new List<string> { "$id", "$revision", "name", "email" };
        Assert.Equal(expected.OrderBy(x => x), result!.OrderBy(x => x));
    }

    /// <summary>
    /// EnsureMinimumFields メソッドが、必須フィールドの一部が既に存在する場合に、残りの必須フィールドを追加することを確認するテスト。
    /// </summary>
    [Fact]
    public void EnsureMinimumFieldsHandlesPartialPresenceGracefully() {
        var input = new List<string> { "$revision", "created_time" };
        var result = InvokeEnsureMinimumFields(input);
        var expected = new List<string> { "$id", "$revision", "created_time" };
        Assert.Equal(expected.OrderBy(x => x), result!.OrderBy(x => x));
    }
    #endregion

    // テスト用に private メソッドを internal に変更 or Reflection 利用
    /// <summary>
    /// EnsureMinimumFields メソッドをリフレクションで呼び出すヘルパーメソッド。
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static IList<string>? InvokeEnsureMinimumFields(IList<string> input) =>
        typeof(KintoneRequestBuilder)
            .GetMethod("EnsureMinimumFields", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { input }) as IList<string>;
}
