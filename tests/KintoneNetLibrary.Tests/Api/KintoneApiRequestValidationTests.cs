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
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

public class KintoneApiRequestValidationTests {
    #region <<Test methods>>
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

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, httpClient);

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

    [Fact]
    public async Task DownloadFileStreamAsyncReturnsCorrectStreamContent() {
        var dummyContent = "This is a file";
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(dummyContent, Encoding.UTF8, "application/octet-stream")
            };
        });

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, httpClient);
        using var stream = await api.DownloadFileStreamAsync("dummyKey");

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var result = await reader.ReadToEndAsync();

        Assert.Equal(dummyContent, result);
    }
    #endregion
}
