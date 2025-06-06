using System.Net;
using System.Text;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Tests.Helpers;
using RichardSzalay.MockHttp;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

public class KintoneApiDownloadFileTests {
    private const string DummyDomain = "example.cybozu.com";
    private const string DummyFileKey = "validKey";

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

        var api = new KintoneApi(CreateMockAccount(), 123, httpClient);

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

        var api = new KintoneApi(CreateMockAccount(), 123, httpClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync("invalidKey"));
        Assert.Equal("指定された fileKey が無効です", ex.Message);
        Assert.Equal("GAIA_CO01", ex.Error?.Code);
    }
    [Fact]
    public async Task DownloadFileAsync_ValidKey_ReturnsExpectedBytes() {
        // Arrange
        var expectedContent = Encoding.UTF8.GetBytes("dummy content");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .WithHeaders("X-Cybozu-API-Token", "dummyToken")
            .Respond(req => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(expectedContent) });

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://dummy.domain/k/v1/");
        var api = new KintoneApi(CreateMockAccount(), appID: 1, httpClient: httpClient);

        // Act
        var result = await api.DownloadFileAsync(DummyFileKey);

        // Assert
        Assert.Equal(expectedContent, result);
    }
    [Fact]
    public async Task DownloadFileStreamAsync_ValidKey_ReturnsExpectedStream() {
        // Arrange
        var expectedText = "stream content";
        var expectedBytes = Encoding.UTF8.GetBytes(expectedText);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .WithHeaders("X-Cybozu-API-Token", "dummyToken")
            .Respond(req => new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent(expectedBytes)
            });

        var httpClient = mockHttp.ToHttpClient();

        var api = new KintoneApi(CreateMockAccount(), appID: 1, httpClient: httpClient);

        // Act
        using var stream = await api.DownloadFileStreamAsync(DummyFileKey);
        using var reader = new StreamReader(stream);
        var actualText = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(expectedText, actualText);
    }
    [Fact]
    public async Task DownloadFileAsync_ErrorResponse_ThrowsKintoneException() {
        // Arrange
        var errorJson = "{\"message\":\"Invalid fileKey\"}";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond(HttpStatusCode.BadRequest, "application/json", errorJson);

        var httpClient = mockHttp.ToHttpClient();
        var api = new KintoneApi(CreateMockAccount(), appID: 1, httpClient: httpClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync(DummyFileKey));
        Assert.Contains("Invalid fileKey", ex.Message);
    }

    #region <<Private method(s)>>
    private static KintoneAccount CreateMockAccount(string apiToken = "dummyToken", string domain = DummyDomain) {
        return new KintoneAccount {
            Domain = domain,
            ApiToken = apiToken
        };
    }
    #endregion
}
