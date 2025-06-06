using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Tests.Helpers;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

public partial class KintoneApiFileUploadTests {
    [Fact]
    public async Task UploadFileAsync_ReturnsFileKey_WhenSuccess() {
        // Arrange
        var expectedFileKey = "abcdef123456";
        var responseJson = JsonSerializer.Serialize(new { fileKey = expectedFileKey });

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            // レスポンス作成
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, 123, httpClient);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Dummy Content"));

        // Act
        var fileKey = await api.UploadFileAsync(stream, "test.txt");

        // Assert
        Assert.Equal(expectedFileKey, fileKey);
    }
    [Fact]
    public async Task UploadFileAsync_ThrowsKintoneException_WhenError() {
        // Arrange
        var errorJson = """
        {
            "message": "ファイルサイズが制限を超えています",
            "code": "GAIA_CO02",
            "id": "error-id-456",
            "summary": "アップロードできるファイルサイズを超えました"
        }
        """;

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            return new HttpResponseMessage(HttpStatusCode.BadRequest) {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, 123, httpClient);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Too large!"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "largefile.zip"));

        Assert.Equal("アップロードできるファイルサイズを超えました", ex.Message);
        Assert.Equal("GAIA_CO02", ex.Error?.Code);
        Assert.Equal("error-id-456", ex.Error?.ID);
    }
    [Fact]
    public async Task UploadFileAsync_Throws_WhenFileSizeExceedsMaxUploadFileSize() {
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\": \"dummyKey\"}")
            });

        var api = new KintoneApi(new KintoneAccount { Domain = "example", ApiToken = "dummy" }, 1, httpClient) {
            MaxUploadFileSize = 10 // 非常に小さく設定
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("これは12バイト"));

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "dummy.txt"));
        Assert.Equal("LOCAL_FILE_TOO_LARGE", ex.Error.Code);
    }
    [Fact]
    public async Task UploadFileAsync_ThrowsException_WhenFileSizeExceedsLimit() {
        // Arrange
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            throw new InvalidOperationException("HTTPリクエストは呼ばれないはずです"));

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, 123, httpClient);

        // 制限値を意図的に小さくする（10バイト）
        api.MaxUploadFileSize = 10;

        // 内容が制限値（10バイト）を超えるデータ
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("これは11バイト以上の内容です"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "test.txt"));

        Assert.Equal("LOCAL_FILE_TOO_LARGE", ex.Error.Code);
        Assert.Contains("ファイルサイズが制限", ex.Error.Message);
    }
    [Fact]
    public async Task UploadFileAsync_ThrowsException_WhenStreamIsNotSeekable() {
        using var stream = new NonSeekableStream();

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ => {
            throw new InvalidOperationException("送信される前に例外が発生するため、このコードは到達しないはずです。");
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, appID: 123, httpClient: httpClient);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => api.UploadFileAsync(stream, "dummy.txt"));

        Assert.Contains("シーク可能なストリーム", ex.Message);
    }
    [Fact]
    public async Task UploadFileAsync_UsingSlowStream_DoesNotThrow() {
        var content = Encoding.UTF8.GetBytes("test slow stream content");
        using var slowStream = new SlowStream(content, delayMilliseconds: 50);

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var responseJson = "{\"fileKey\":\"dummy_file_key\"}";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            return responseMessage;
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, 123, httpClient);
        var ex = await Record.ExceptionAsync(() => api.UploadFileAsync(slowStream, "slow.txt"));

        Assert.Null(ex);
    }
    [Fact]
    public async Task UploadFileAsync_UsingFaultyStream_ThrowsIOException() {
        var content = Encoding.UTF8.GetBytes("test faulty stream content");
        using var faultyStream = new FaultyStream(content, failAfterBytes: 10);

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ => {
            // モックレスポンスは成功にしておく（例外を検出する目的なので）
            var json = "{\"fileKey\":\"dummy_file_key\"}";
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, 123, httpClient);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => api.UploadFileAsync(faultyStream, "faulty.txt"));

        Assert.IsType<IOException>(ex.InnerException); // 内部例外が IOException であることを確認
        Assert.Contains("意図的な例外", ex.InnerException?.Message);
    }
    [Fact]
    public async Task UploadFileAsync_UsingEmptyStream_ThrowsInvalidOperationException() {
        using var emptyStream = new EmptyStream(); // Length == 0 のストリーム

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            throw new InvalidOperationException("このコードには到達しないはずです。"));

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, 123, httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            api.UploadFileAsync(emptyStream, "empty.txt"));
    }
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "400")]
    [InlineData(HttpStatusCode.InternalServerError, "500")]
    public async Task UploadFileAsync_WhenHttpResponseIsError_ThrowsKintoneException(HttpStatusCode statusCode, string expectedCode) {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy content"));

        var errorJson = """
        {
        "message": "アップロード失敗",
        "code": "SAMPLE_ERROR_CODE",
        "id": "sample-id" }
        """;

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            new HttpResponseMessage(statusCode) {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
            });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyAppId", ApiToken = "dummyToken" }, 123, httpClient);

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "error.txt"));

        Assert.Equal("SAMPLE_ERROR_CODE", ex.Error.Code);
        Assert.Equal("アップロード失敗", ex.Error.Message);
    }
    [Fact]
    public async Task UploadFileAsync_WithSpecialFileName_WorksCorrectly() {
        var expectedFileKey = "special_key";
        var fileName = "テスト😊&記号.txt";

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            // request.ContentをMultipartContentとしてキャスト
            var multipartContent = request.Content as MultipartContent;
            Assert.NotNull(multipartContent);

            bool found = false;

            foreach (var part in multipartContent) {
                var contentDisposition = part.Headers.ContentDisposition;
                if (contentDisposition != null && contentDisposition.FileName != null) {
                    var rawFileName = contentDisposition.FileName.Trim('"');

                    // MIME Base64 エンコードされたファイル名のデコード
                    string decodedFileName = rawFileName;
                    if (rawFileName.StartsWith("=?utf-8?B?", StringComparison.OrdinalIgnoreCase)) {
                        var base64Part = rawFileName.Substring(10, rawFileName.Length - 14);
                        var bytes = Convert.FromBase64String(base64Part);
                        decodedFileName = Encoding.UTF8.GetString(bytes);
                    }

                    // filename* があればそちらを優先的にURIデコード
                    if (!string.IsNullOrEmpty(contentDisposition.FileNameStar)) {
                        decodedFileName = Uri.UnescapeDataString(contentDisposition.FileNameStar);
                    }

                    Assert.Equal(fileName, decodedFileName);
                    found = true;
                    break;
                }
            }

            Assert.True(found, "ファイル部分のContent-Dispositionヘッダーが見つかりませんでした。");

            // レスポンスを返す
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent($"{{\"fileKey\":\"{expectedFileKey}\"}}", Encoding.UTF8, "application/json")
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummyDomain", ApiToken = "dummyToken" }, 123, httpClient);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));
        var fileKey = await api.UploadFileAsync(stream, fileName);

        Assert.Equal(expectedFileKey, fileKey);
    }
    [Fact]
    public async Task UploadFileAsync_NullStream_ThrowsArgumentNullException() {
        var api = new KintoneApi(new KintoneAccount { Domain = "dummyDomain", ApiToken = "dummyToken" }, 123);

        Stream? nullStream = null;
        var fileName = "dummy.txt";

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await api.UploadFileAsync(nullStream!, fileName);
        });

        Assert.Contains("stream", ex.ParamName);
    }
    [Fact]
    public async Task UploadFileAsync_ResponseWithNullFileKey_ThrowsInvalidOperationException() {
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var jsonWithNullFileKey = @"{ ""fileKey"": null }"; // fileKey が null
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(jsonWithNullFileKey, Encoding.UTF8, "application/json")
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummy", ApiToken = "dummyToken" }, 123, httpClient);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));
        var fileName = "file.txt";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            api.UploadFileAsync(stream, fileName));

        Assert.Contains("fileKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact(DisplayName = "UploadFileAsync: 非常に長いファイル名（255バイト以上）でも正常にアップロードされる")]
    public async Task UploadFileAsync_WithVeryLongFileName_WorksCorrectly() {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));

        // ファイル名：300バイト以上（日本語2バイト + 拡張子）
        var longFileName = new string('あ', 120) + ".txt"; // 約360バイト（UTF-8）

        var dummyFileKey = "dummy_file_key";

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var body = request.Content.ReadAsStringAsync().Result;

            // ファイル名（通常とエンコードされた形式）を検出
            var plainFileName = Path.GetFileName(longFileName);
            var encodedFileName = Uri.EscapeDataString(plainFileName);

            Assert.True(
                body.Contains(plainFileName) || body.Contains(encodedFileName),
                $"Request body does not contain expected file name. Actual body: {body}"
            );

            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(JsonSerializer.Serialize(new { fileKey = "dummy_file_key" }))
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummy", ApiToken = "dummyToken" }, 123, httpClient);

        // Act
        var result = await api.UploadFileAsync(stream, longFileName);

        // Assert
        Assert.Equal(dummyFileKey, result);
    }
    [Fact(DisplayName = "UploadFileAsync_ResetsStreamPosition_BeforeUpload")]
    public async Task UploadFileAsync_ResetsStreamPosition_BeforeUpload() {
        // Arrange
        const string fileName = "positioned.txt";
        var originalContent = "0123456789";
        var expectedUploadContent = "0123456789";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));

        // Position を途中にしておく（例：先頭3バイト読み飛ばす位置）
        stream.Position = 3;

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var body = request.Content.ReadAsStringAsync().Result;

            // 中身がストリーム全体（0123456789）であることを確認
            Assert.Contains(expectedUploadContent, body);

            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\":\"dummy_file_key\"}")
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummy", ApiToken = "dummyToken" }, 123, httpClient);

        // Act
        var fileKey = await api.UploadFileAsync(stream, fileName);

        // Assert
        Assert.Equal("dummy_file_key", fileKey);
    }
    [Theory(DisplayName = "UploadFileAsync_WithControlCharactersInFileName_WorksCorrectly")]
    [InlineData("test\n.txt")]
    [InlineData("test\r.txt")]
    public async Task UploadFileAsync_WithControlCharactersInFileName_WorksCorrectly(string fileName) {
        // Arrange
        var dummyContent = "dummy";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(dummyContent));

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var body = request.Content.ReadAsStringAsync().Result;

            // Content-Disposition ヘッダーに filename が含まれていること
            Assert.Contains("Content-Disposition", body);
            Assert.Contains("form-data", body);
            Assert.Contains("filename", body);

            // Content-Disposition の1行目を抽出（改行まで）
            var matchHeader = ContentDispositionRegex().Match(body);
            Assert.True(matchHeader.Success, "Content-Disposition ヘッダーが見つかりません");

            var contentDispositionLine = matchHeader.Value;

            // filename="..." を抽出
            // var matchFilename = Regex.Match(contentDispositionLine, @"filename=""([^""]*)""");
            var matchFilename = FileNameRegex().Match(contentDispositionLine);
            Assert.True(matchFilename.Success, "filename= の形式で抽出できませんでした");

            var sanitizedFileName = matchFilename.Groups[1].Value;

            Assert.DoesNotContain("\r", sanitizedFileName);
            Assert.DoesNotContain("\n", sanitizedFileName);

            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\":\"dummy_file_key\"}")
            };
        });

        var api = new KintoneApi(
            new KintoneAccount { Domain = "dummy", ApiToken = "dummyToken" },
            123,
            httpClient
        );

        // Act
        var fileKey = await api.UploadFileAsync(stream, fileName);

        // Assert
        Assert.Equal("dummy_file_key", fileKey);
    }
    [Theory(DisplayName = "UploadFileAsync_ContentTypeIsApplicationOctetStream_WhenFileNameIsNullOrEmptyOrNull")]
    [InlineData(null)]      // null もテスト対象に追加
    [InlineData("")]
    [InlineData(" ")]
    public async Task UploadFileAsync_ContentTypeIsApplicationOctetStream_WhenFileNameIsNullOrEmptyOrNull(string? testFileName) {
        // Arrange
        var dummyContent = "dummy";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(dummyContent));

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var multipartContent = request.Content as MultipartFormDataContent;
            Assert.NotNull(multipartContent);

            foreach (var content in multipartContent) {
                if (content is StreamContent streamContent) {
                    var contentType = streamContent.Headers.ContentType?.MediaType;
                    Assert.Equal("application/octet-stream", contentType);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\":\"dummy_file_key\"}")
            };
        });

        var api = new KintoneApi(new KintoneAccount { Domain = "dummy", ApiToken = "dummyToken" }, 123, httpClient);

        // Act
        var fileKey = await api.UploadFileAsync(stream, testFileName);

        // Assert
        Assert.Equal("dummy_file_key", fileKey);
    }

    [GeneratedRegex(@"Content-Disposition: form-data;[^\r\n]*")]
    private static partial Regex ContentDispositionRegex();
    [GeneratedRegex(@"filename=(?:""([^""]*)""|([^;]*))")]
    private static partial Regex FileNameRegex();
}
