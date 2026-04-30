using System.IO.Enumeration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Tests.Helpers;
using Moq;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

/// <summary>
/// Tests for validating the construction of API requests, particularly for file upload and download operations.
/// </summary>
public class KintoneApiRequestValidationTests {
    #region <<Test methods>>
    /// <summary>
    /// Tests that the UploadFileAsync method constructs the multipart/form-data request correctly, including the content type and field name for the file.
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncSetsCorrectContentTypeAndFieldName() {
        HttpRequestMessage? capturedRequest = null;

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            capturedRequest = request;

            var responseJson = JsonSerializer.Serialize(new { fileKey = "testKey123" });
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test Content"));
        await api.UploadFileAsync(stream, "sample.txt");

        Assert.NotNull(capturedRequest);

        await capturedRequest!.Content!.LoadIntoBufferAsync();
        var multipart = capturedRequest!.Content as MultipartFormDataContent;
        Assert.NotNull(multipart);

        // var fileContent = multipart!.FirstOrDefault();
        var fileContent = multipart!.FirstOrDefault(c => c.Headers.ContentDisposition?.Name?.Trim('"') == "file");
        Assert.NotNull(fileContent);

        var disposition = fileContent!.Headers.ContentDisposition;
        Assert.Equal("form-data", disposition?.DispositionType);
        Assert.Equal("file", disposition?.Name?.Trim('"'));
        Assert.Equal("sample.txt", disposition?.FileName?.Trim('"'));

        Assert.Equal("application/octet-stream", fileContent!.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Tests that the DownloadFileStreamAsync method correctly processes the response and returns a stream with the expected content, ensuring that the content type is handled appropriately.
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncReturnsCorrectStreamContent() {
        var dummyContent = "This is a file";
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(dummyContent, Encoding.UTF8, "application/octet-stream")
            };
        });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);
        using var stream = await api.DownloadFileStreamAsync("dummyKey");

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var result = await reader.ReadToEndAsync();

        Assert.Equal(dummyContent, result);
    }
    #endregion
}
