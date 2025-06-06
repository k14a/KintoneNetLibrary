using System.Net;
using System.Text;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Tests.Helpers;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

public class KintoneApiDownloadFileTests {
    [Fact]
    public async Task DownloadFileAsync_ReturnsByteArray_WhenSuccess() {
        // Arrange
        var expectedContent = Encoding.UTF8.GetBytes("Hello Kintone!");
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent(expectedContent)
            };
            return response;
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "example.kintone.com", ApiToken = "dummy" }, 123, httpClient);

        // Act
        var result = await api.DownloadFileAsync("dummyFileKey");

        // Assert
        Assert.Equal(expectedContent, result);
    }

    [Fact]
    public async Task DownloadFileAsync_ThrowsKintoneException_WhenError() {
        // Arrange
        var errorJson = """
        {
            "message": "ファイルが存在しません",
            "code": "GAIA_CO01",
            "id": "some-id",
            "summary": "指定された fileKey が無効です"
        }
        """;

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound) {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
            };
            return response;
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "example.kintone.com", ApiToken = "dummy" }, 123, httpClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync("invalidKey"));
        Assert.Equal("指定された fileKey が無効です", ex.Message);
        Assert.Equal("GAIA_CO01", ex.Error?.Code);
    }
}
