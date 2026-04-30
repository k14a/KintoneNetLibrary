using System.Net;
using System.Net.Http.Headers;
using System.Text;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Tests.Helpers;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

/// <summary>
/// KintoneApiのファイルダウンロード機能に関するテストクラス。
/// </summary>
public class KintoneApiDownloadFileTests {
    private const string DummyDomain = "example.cybozu.com";
    private const string DummyFileKey = "validKey";

    /// <summary>
    /// DownloadFileAsyncが正常にファイルのバイト配列を返すことを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncReturnsByteArrayWhenSuccess() {
        // Arrange
        var expectedContent = Encoding.UTF8.GetBytes("Hello Kintone!");
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent(expectedContent)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return response;
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 123, factoryMock.Object);

        // Act
        var result = await api.DownloadFileAsync("dummyFileKey");

        // Assert
        Assert.Equal(expectedContent, result);
    }

    /// <summary>
    /// DownloadFileAsyncがエラー時にKintoneExceptionをスローし、エラーメッセージとコードが正しく設定されていることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncThrowsKintoneExceptionWhenError() {
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

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 123, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync("invalidKey"));
        Assert.Equal("指定された fileKey が無効です", ex.Message);
        Assert.Equal("GAIA_CO01", ex.Error?.Code);
    }

    /// <summary>
    /// DownloadFileAsyncが有効なfileKeyで正しいバイト配列を返すことを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncValidKeyReturnsExpectedBytes() {
        // Arrange
        var expectedContent = Encoding.UTF8.GetBytes("dummy content");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .WithHeaders("X-Cybozu-API-Token", "dummyToken")
            .Respond(req => {
                var content = new ByteArrayContent(expectedContent);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            });

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://dummy.domain/k/v1/");

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act
        var result = await api.DownloadFileAsync(DummyFileKey);

        // Assert
        Assert.Equal(expectedContent, result);
    }

    /// <summary>
    /// DownloadFileStreamAsyncが有効なfileKeyで正しいストリームを返すことを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncValidKeyReturnsExpectedStream() {
        // Arrange
        var expectedText = "stream content";
        var expectedBytes = Encoding.UTF8.GetBytes(expectedText);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .WithHeaders("X-Cybozu-API-Token", "dummyToken")
            .Respond(req => {
                var content = new ByteArrayContent(expectedBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            });

        var httpClient = mockHttp.ToHttpClient();

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act
        using var stream = await api.DownloadFileStreamAsync(DummyFileKey);
        using var reader = new StreamReader(stream);
        var actualText = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(expectedText, actualText);
    }

    /// <summary>
    /// DownloadFileAsyncがエラー時にKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncErrorResponseThrowsKintoneException() {
        // Arrange
        var errorJson = "{\"message\":\"Invalid fileKey\"}";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond(HttpStatusCode.BadRequest, "application/json", errorJson);

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync(DummyFileKey));
        Assert.Contains("Invalid fileKey", ex.Message);
    }

    /// <summary>
    /// DownloadFileAsyncがnullまたは空のfileKeyでArgumentNullExceptionまたはArgumentExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncNullFileKeyThrowsArgumentNullException() {
        var httpClient = new HttpClient(); // 実際に送信されない
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => api.DownloadFileAsync(null));
        Assert.Contains("fileKey", ex.Message);
    }

    /// <summary>
    /// DownloadFileAsyncが空のfileKeyでArgumentExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncEmptyFileKeyThrowsArgumentException() {
        var httpClient = new HttpClient(); // 実際に送信されない
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => api.DownloadFileAsync(""));
        Assert.Contains("fileKey", ex.Message);
    }

    /// <summary>
    /// DownloadFileAsyncがHTTPエラーでKintoneExceptionをスローし、エラーメッセージにAPIからのエラー内容が含まれていることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncInvalidFileKeyThrowsKintoneException() {
        // Arrange
        var errorJson = "{\"message\":\"Invalid fileKey\"}";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond(HttpStatusCode.BadRequest, "application/json", errorJson);

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync(DummyFileKey));
        Assert.Contains("Invalid fileKey", ex.Message);
    }

    /// <summary>
    /// DownloadFileAsyncがHTTPステータスコード200であっても、APIからのエラーを示すJSONが返された場合にKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncErrorJsonReturnedThrowsKintoneException() {
        var errorJson = "{\"message\":\"File not found\"}";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond("application/json", errorJson); // ← ステータスコード200だがJSON

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync(DummyFileKey));
        Assert.Contains("File not found", ex.Message);
    }

    /// <summary>
    /// DownloadFileAsyncがHTTPステータスコード200であっても、APIからのエラーを示すJSONが返された場合にKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncInvalidJsonStructureThrowsKintoneException() {
        var invalidJson = "{ this is not valid json }";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond("application/json", invalidJson);

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync(DummyFileKey));
        Assert.Contains("JSON", ex.Message); // メッセージ内容はKintoneErrorConverter次第で調整
    }

    /// <summary>
    /// DownloadFileAsyncがHTTPステータスコード200であっても、Content-Typeが想定外の場合にKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncUnexpectedContentTypeThrowsKintoneException() {
        var errorJson = "{\"message\":\"Unexpected response\"}";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond("text/html", errorJson); // Content-Typeが想定外

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync(DummyFileKey));
        Assert.Contains("Content-Type", ex.Message); // もしくは "Unexpected response"
    }

    /// <summary>
    /// DownloadFileAsyncがHTTPリクエストのタイムアウトでTaskCanceledExceptionをスローした場合に、KintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsyncHttpTimeoutThrowsKintoneException() {
        // Arrange
        var handler = new StubHttpMessageHandler(async _ => {
            // 疑似的にタイムアウトを発生させる
            await Task.Delay(1);
            throw new TaskCanceledException("タイムアウト模擬");
        });

        var httpClient = new HttpClient(handler);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileAsync(DummyFileKey));
        Assert.Contains("タイムアウト", ex.Message); // 実装に応じて調整
    }

    /// <summary>
    /// DownloadFileStreamAsyncが有効なfileKeyで正しいストリームを返すことを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncValidResponseReturnsStream() {
        // Arrange
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var mockHttp = new MockHttpMessageHandler();

        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new ByteArrayContent(fileBytes)
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond(_ => response);

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act
        using var stream = await api.DownloadFileStreamAsync(DummyFileKey);

        // Assert
        Assert.NotNull(stream);
        var buffer = new byte[fileBytes.Length];
        var readCount = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(fileBytes.Length, readCount);
        Assert.Equal(fileBytes, buffer);
    }

    /// <summary>
    /// DownloadFileStreamAsyncがHTTPエラーでKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncErrorResponseThrowsKintoneException() {
        // Arrange
        var errorJson = "{\"message\":\"Invalid fileKey\"}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond(HttpStatusCode.BadRequest, "application/json", errorJson);

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileStreamAsync(DummyFileKey));
        Assert.Contains("Invalid fileKey", ex.Message);
    }

    /// <summary>
    /// DownloadFileStreamAsyncがnullまたは空のfileKeyでArgumentNullExceptionまたはArgumentExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncNullFileKeyThrowsArgumentNullException() {
        var httpClient = new HttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => api.DownloadFileStreamAsync(null!));
        Assert.Contains("fileKey", ex.Message);
    }

    /// <summary>
    /// DownloadFileStreamAsyncが空のfileKeyでArgumentExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncEmptyFileKeyThrowsArgumentException() {
        var httpClient = new HttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => api.DownloadFileStreamAsync(string.Empty));
        Assert.Contains("fileKey must not be empty", ex.Message);
    }

    /// <summary>
    /// DownloadFileStreamAsyncがHTTPステータスコード200であっても、Content-Typeが想定外の場合にKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncUnexpectedContentTypeThrowsKintoneException() {
        // Arrange
        var errorHtml = "<html><body>Error</body></html>";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json?fileKey={DummyFileKey}")
            .Respond("text/html", errorHtml);

        var httpClient = mockHttp.ToHttpClient();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileStreamAsync(DummyFileKey));
        Assert.Contains("予期しないContent-Type", ex.Message);
    }

    /// <summary>
    /// DownloadFileStreamAsyncが有効なfileKeyで正しいストリームを返すことを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncValidFileKeyReturnsStream() {
        var contentBytes = new byte[] { 1, 2, 3 };
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json*")
                .Respond(request => new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(contentBytes) {
                        Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
                    }
                });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);
        var stream = await api.DownloadFileStreamAsync("valid_file_key");

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.Equal(contentBytes, ms.ToArray());
    }

    /// <summary>
    /// DownloadFileStreamAsyncがHTTPステータスコード200であっても、Content-Typeが想定外の場合にKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncInvalidContentTypeThrowsKintoneExceptionWithContentTypeInMessage() {
        // Arrange
        var htmlBody = "<html><body>Error Page</body></html>";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json*")
            .Respond("text/html", htmlBody);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileStreamAsync("invalid_key"));
        Assert.Contains("Content-Type", ex.Message); // Content-Typeの記述が含まれていること
        Assert.Contains("text/html", ex.Message);    // 実際に受信したContent-Typeが含まれていること
    }

    /// <summary>
    /// DownloadFileStreamAsyncがHTTPステータスコード200であっても、APIからのエラーを示すJSONが返された場合にKintoneExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncKintoneJsonErrorThrowsKintoneExceptionWithErrorMessage() {
        // Arrange
        var jsonError = "{\"code\":\"E001\",\"message\":\"Invalid fileKey\"}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json*")
            .Respond("application/json", jsonError);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileStreamAsync("invalid_key"));
        Assert.Contains("Invalid fileKey", ex.Message); // Kintoneのエラーメッセージが含まれていること
    }

    /// <summary>
    /// DownloadFileStreamAsyncがHTTPエラーでKintoneExceptionをスローし、APIからのエラーコードがKintoneException.Errorプロパティに正しく設定されていることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncHttpErrorWithJsonThrowsParsedKintoneException() {
        var jsonError = "{\"code\":\"E001\",\"message\":\"Invalid fileKey\"}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json*")
                .Respond(HttpStatusCode.BadRequest, "application/json", jsonError);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileStreamAsync("invalid_key"));
        Assert.NotNull(ex.Error);
        Assert.Equal("E001", ex.Error.Code);
    }

    /// <summary>
    /// DownloadFileStreamAsyncがHTTPエラーでKintoneExceptionをスローし、APIからのレスポンスがJSONでない場合に、Content-Typeを含む一般的なエラーメッセージがKintoneExceptionに含まれていることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncHttpErrorWithNonJsonBodyThrowsGenericKintoneException() {
        var badBody = "<html>Internal Error</html>";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json*")
                .Respond(HttpStatusCode.InternalServerError, "text/html", badBody);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.DownloadFileStreamAsync("filekey"));
        Assert.Contains("Content-Type", ex.Message);
    }

    /// <summary>
    /// DownloadFileStreamAsyncが空のfileKeyでArgumentExceptionをスローすることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncEmptyFileKeyEncodedProperly() {
        var contentBytes = new byte[] { 1 };
        var capturedUri = string.Empty;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json*")
                .Respond(request => {
                    capturedUri = request.RequestUri?.ToString() ?? "";
                    return new HttpResponseMessage {
                        StatusCode = HttpStatusCode.OK,
                        Content = new ByteArrayContent(contentBytes) {
                            Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
                        }
                    };
                });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => api.DownloadFileStreamAsync(""));
        Assert.Contains("fileKey must not be empty.", ex.Message);
    }

    /// <summary>
    /// DownloadFileStreamAsyncがfileKeyに特殊文字を含む場合に、URLエンコードされたfileKeyがリクエストURIに含まれていることを確認するテスト。
    /// </summary>
    [Fact]
    public async Task DownloadFileStreamAsyncFileKeyWithSpecialCharsEncodedInUrl() {
        var fileKey = "abc+/=def";
        var encoded = Uri.EscapeDataString(fileKey);
        var actualUri = "";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"https://{DummyDomain}/k/v1/file.json*")
                .Respond(req => {
                    actualUri = req.RequestUri!.ToString();
                    return new HttpResponseMessage {
                        StatusCode = HttpStatusCode.OK,
                        Content = new ByteArrayContent(new byte[] { 1 }) {
                            Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
                        }
                    };
                });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var api = new KintoneApi(CreateMockAccess(), 1, factoryMock.Object);
        await api.DownloadFileStreamAsync(fileKey);

        Assert.Contains($"fileKey={encoded}", actualUri);
    }

    #region <<Private method(s)>>
    /// <summary>
    /// テスト用のApiTokenAccessを作成するヘルパーメソッド。
    /// </summary>
    /// <param name="apiToken">APIトークン</param>
    /// <param name="domain">ドメイン</param>
    /// <returns>ApiTokenAccessのインスタンス</returns>
    private static ApiTokenAccess CreateMockAccess(string apiToken = "dummyToken", string domain = DummyDomain) {
        return new ApiTokenAccess(domain, apiToken);
    }
    #endregion
}
