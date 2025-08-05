using System.Reflection;
using KintoneNetLibrary.Infrastructure.Helpers;
using Xunit;

namespace KintoneNetLibrary.Tests.Helpers;

public class KintoneRequestBuilderTests {
    #region <<Test methods>>
    [Fact]
    public void BuildFindRequestUri_WithoutQueryOrFields_ReturnsAppOnly() {
        var uri = KintoneRequestBuilder.BuildFindRequestUri(new Uri("https://example.cybozu.com"), "/v1/records.json", 123);
        Assert.Equal("https://example.cybozu.com/v1/records.json?app=123", uri.ToString());
    }

    [Fact]
    public void BuildFindRequestUri_WithQueryAndFields_ReturnsFullUri() {
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
    [Theory]
    [InlineData(null, null)]
    [InlineData(new string[] { }, null)]
    public void EnsureMinimumFields_ReturnsNull_WhenInputIsNullOrEmpty(string[] input, string[] expected) {
        var result = InvokeEnsureMinimumFields(input);
        Assert.Null(result);
    }
    [Fact]
    public void EnsureMinimumFields_AddsRequiredFields_WhenMissing() {
        var input = new List<string> { "name", "email" };
        var result = InvokeEnsureMinimumFields(input);
        var expected = new List<string> { "$id", "$revision", "name", "email" };
        Assert.Equal(expected.OrderBy(x => x), result.OrderBy(x => x));
    }
    [Fact]
    public void EnsureMinimumFields_HandlesPartialPresence_Gracefully() {
        var input = new List<string> { "$revision", "created_time" };
        var result = InvokeEnsureMinimumFields(input);
        var expected = new List<string> { "$id", "$revision", "created_time" };
        Assert.Equal(expected.OrderBy(x => x), result.OrderBy(x => x));
    }
    #endregion

    // テスト用に private メソッドを internal に変更 or Reflection 利用
    private static IList<string> InvokeEnsureMinimumFields(IList<string> input) =>
        typeof(KintoneRequestBuilder)
            .GetMethod("EnsureMinimumFields", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { input }) as IList<string>;
}
