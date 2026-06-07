using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Tests.Helpers;
using Moq;
using Moq.Protected;
using Xunit;

namespace KintoneNetLibrary.Tests.Api;

/// <summary>
/// KintoneApi のファイルアップロード機能に関するユニットテストクラス。
/// </summary>
public partial class KintoneApiFileUploadTests {
    /// <summary>
    /// UploadFileAsync メソッドが正常にファイルをアップロードし、APIから返された fileKey を正しく返すことを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncReturnsFileKeyWhenSuccess() {
        // Arrange
        var expectedFileKey = "abcdef123456";
        var responseJson = JsonSerializer.Serialize(new { fileKey = expectedFileKey });

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            // レスポンス作成
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Dummy Content"));

        // Act
        var fileKey = await api.UploadFileAsync(stream, "test.txt");

        // Assert
        Assert.Equal(expectedFileKey, fileKey);
    }

    /// <summary>
    /// UploadFileAsync メソッドがエラー時に KintoneException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsKintoneExceptionWhenApiReturnsError() {
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

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Too large!"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "largefile.zip"));

        Assert.Equal("アップロードできるファイルサイズを超えました", ex.Message);
        Assert.Equal("GAIA_CO02", ex.Error?.Code);
        Assert.Equal("error-id-456", ex.Error?.Id);
    }

    /// <summary>
    /// UploadFileAsync メソッドがローカルでファイルサイズの制限を超えた場合に KintoneException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsWhenFileSizeExceedsMaxUploadFileSize() {
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\": \"dummyKey\"}")
            });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object) {
            MaxUploadFileSize = 10 // 非常に小さく設定
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("これは12バイト"));

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "dummy.txt"));
        Assert.Equal("LOCAL_FILE_TOO_LARGE", ex.Error!.Code);
    }

    /// <summary>
    /// UploadFileAsync メソッドがローカルでファイルサイズの制限を超えた場合に、適切なエラーメッセージとコードを持つ KintoneException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsExceptionWhenFileSizeExceedsLimit() {
        // Arrange
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            throw new InvalidOperationException("HTTPリクエストは呼ばれないはずです"));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object) {
            // 制限値を意図的に小さくする（10バイト）
            MaxUploadFileSize = 10
        };

        // 内容が制限値（10バイト）を超えるデータ
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("これは11バイト以上の内容です"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "test.txt"));

        Assert.Equal("LOCAL_FILE_TOO_LARGE", ex.Error!.Code);
        Assert.Contains("ファイルサイズが制限", ex.Error!.Message);
    }

    /// <summary>
    /// UploadFileAsync メソッドがシーク不可能なストリームを受け取った場合に InvalidOperationException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncThrowsExceptionWhenStreamIsNotSeekable() {
        using var stream = new NonSeekableStream();

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ => {
            throw new InvalidOperationException("送信される前に例外が発生するため、このコードは到達しないはずです。");
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => api.UploadFileAsync(stream, "dummy.txt"));

        Assert.Contains("シーク可能なストリーム", ex.Message);
    }

    /// <summary>
    /// UploadFileAsync メソッドが遅延ストリームを正常に処理できることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncUsingSlowStreamDoesNotThrow() {
        var content = Encoding.UTF8.GetBytes("test slow stream content");
        using var slowStream = new SlowStream(content, delayMilliseconds: 50);

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var responseJson = "{\"fileKey\":\"dummy_file_key\"}";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            return responseMessage;
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);
        var ex = await Record.ExceptionAsync(() => api.UploadFileAsync(slowStream, "slow.txt"));

        Assert.Null(ex);
    }

    /// <summary>
    /// UploadFileAsync メソッドが読み込み中に例外をスローするストリームを受け取った場合に、HttpRequestException をスローし、その内部例外が IOException であることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncUsingFaultyStreamThrowsIOException() {
        var content = Encoding.UTF8.GetBytes("test faulty stream content");
        using var faultyStream = new FaultyStream(content, failAfterBytes: 10);

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ => {
            // モックレスポンスは成功にしておく（例外を検出する目的なので）
            var json = "{\"fileKey\":\"dummy_file_key\"}";
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => api.UploadFileAsync(faultyStream, "faulty.txt"));

        Assert.IsType<IOException>(ex.InnerException); // 内部例外が IOException であることを確認
        Assert.Contains("意図的な例外", ex.InnerException?.Message);
    }

    /// <summary>
    /// UploadFileAsync メソッドが空のストリームを受け取った場合に InvalidOperationException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncUsingEmptyStreamThrowsInvalidOperationException() {
        using var emptyStream = new EmptyStream(); // Length == 0 のストリーム

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            throw new InvalidOperationException("このコードには到達しないはずです。"));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            api.UploadFileAsync(emptyStream, "empty.txt"));
    }

    /// <summary>
    /// UploadFileAsync メソッドが HTTP レスポンスでエラーコードが返された場合に、KintoneException をスローし、そのエラーコードとメッセージが正しく設定されていることを検証するテスト。
    /// </summary>
    /// <param name="statusCode">HTTP レスポンスのステータスコード</param>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task UploadFileAsyncWhenHttpResponseIsErrorThrowsKintoneException(HttpStatusCode statusCode) {
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

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        var ex = await Assert.ThrowsAsync<KintoneException>(() => api.UploadFileAsync(stream, "error.txt"));

        Assert.Equal("SAMPLE_ERROR_CODE", ex.Error!.Code);
        Assert.Equal("アップロード失敗", ex.Error!.Message);
    }

    /// <summary>
    /// UploadFileAsync メソッドが特殊文字を含むファイル名を正しく処理し、APIに正しい形式で送信されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncWithSpecialFileNameWorksCorrectly() {
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

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));
        var fileKey = await api.UploadFileAsync(stream, fileName);

        Assert.Equal(expectedFileKey, fileKey);
    }

    /// <summary>
    /// UploadFileAsync メソッドが null のストリームを受け取った場合に ArgumentNullException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncNullStreamThrowsArgumentNullException() {
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(_ =>
            throw new InvalidOperationException("このコードには到達しないはずです。"));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        Stream? nullStream = null;
        var fileName = "dummy.txt";

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await api.UploadFileAsync(nullStream!, fileName);
        });

        Assert.Contains("stream", ex.ParamName);
    }

    /// <summary>
    /// UploadFileAsync メソッドが API から fileKey が null のレスポンスを受け取った場合に、KintoneException をスローし、エラーコードが "FILEKEY_MISSING" であることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncResponseWithNullFileKeyThrowsKintoneException() {
        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var jsonWithNullFileKey = @"{ ""fileKey"": null }"; // fileKey が null
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(jsonWithNullFileKey, Encoding.UTF8, "application/json")
            };
        });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));
        var fileName = "file.txt";

        var ex = await Assert.ThrowsAsync<KintoneException>(() =>
            api.UploadFileAsync(stream, fileName));

        Assert.Equal("FILEKEY_MISSING", ex.Error!.Code);
        Assert.Contains("fileKey", ex.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// UploadFileAsync メソッドが非常に長いファイル名（255バイト以上）を正しく処理し、APIに正しい形式で送信されることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncWithVeryLongFileNameWorksCorrectly() {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));

        // ファイル名：300バイト以上（日本語2バイト + 拡張子）
        var longFileName = new string('あ', 120) + ".txt"; // 約360バイト（UTF-8）

        var dummyFileKey = "dummy_file_key";

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var body = request.Content!.ReadAsStringAsync().Result;

            // ファイル名（通常とエンコードされた形式）を検出
            var plainFileName = Path.GetFileName(longFileName);
            var encodedFileName = Uri.EscapeDataString(plainFileName);

            Assert.True(
                body.Contains(plainFileName) || body.Contains(encodedFileName),
                $"Request body does not contain expected file name. Actual body: {body}"
            );

            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\":\"dummy_file_key\"}", Encoding.UTF8, "application/json")
            };
            return response;
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        // Act
        var result = await api.UploadFileAsync(stream, longFileName);

        // Assert
        Assert.Equal(dummyFileKey, result);
    }

    /// <summary>
    /// UploadFileAsync メソッドがストリームの Position を途中に設定した状態で渡された場合に、API に送信される前に Position をリセットして全体をアップロードすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncResetsStreamPositionBeforeUpload() {
        // Arrange
        const string fileName = "positioned.txt";
        var originalContent = "0123456789";
        var expectedUploadContent = "0123456789";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));

        // Position を途中にしておく（例：先頭3バイト読み飛ばす位置）
        stream.Position = 3;

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var body = request.Content!.ReadAsStringAsync().Result;

            // 中身がストリーム全体（0123456789）であることを確認
            Assert.Contains(expectedUploadContent, body);

            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\":\"dummy_file_key\"}", Encoding.UTF8, "application/json")
            };
            return response;
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        // Act
        var fileKey = await api.UploadFileAsync(stream, fileName);

        // Assert
        Assert.Equal("dummy_file_key", fileKey);
    }

    /// <summary>
    /// UploadFileAsync メソッドがファイル名に制御文字（例：改行やキャリッジリターン）が含まれている場合に、API に送信される前にこれらの文字を適切に処理して送信することを検証するテスト。
    /// </summary>
    /// <param name="fileName">テスト対象のファイル名</param>
    [Theory]
    [InlineData("test\n.txt")]
    [InlineData("test\r.txt")]
    public async Task UploadFileAsyncWithControlCharactersInFileNameWorksCorrectly(string fileName) {
        // Arrange
        var dummyContent = "dummy";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(dummyContent));

        var httpClient = KintoneHttpTestHelper.CreateMockHttpClient(request => {
            var body = request.Content!.ReadAsStringAsync().Result;

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

            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\":\"dummy_file_key\"}", Encoding.UTF8, "application/json")
            };
            return response;
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        // Act
        var fileKey = await api.UploadFileAsync(stream, fileName);

        // Assert
        Assert.Equal("dummy_file_key", fileKey);
    }

    /// <summary>
    /// UploadFileAsync メソッドがファイル名が null、空文字、またはスペースのみの場合に、API に送信される前に Content-Type を "application/octet-stream" に設定して送信することを検証するテスト。
    /// </summary>
    /// <param name="testFileName">テスト対象のファイル名</param>
    [Theory]
    [InlineData(null)]      // null もテスト対象に追加
    [InlineData("")]
    [InlineData(" ")]
    public async Task UploadFileAsyncContentTypeIsApplicationOctetStreamWhenFileNameIsNullOrEmptyOrNull(string? testFileName) {
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

            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"fileKey\":\"dummy_file_key\"}", Encoding.UTF8, "application/json")
            };
            return response;
        });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        // Act
        var fileKey = await api.UploadFileAsync(stream, testFileName);

        // Assert
        Assert.Equal("dummy_file_key", fileKey);
    }

    /// <summary>
    /// UploadFileAsync メソッドがキャンセルされた CancellationToken を受け取った場合に、TaskCanceledException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncCancellationRequestedThrowsTaskCanceledException() {
        // Arrange
        using var cts = new CancellationTokenSource();
        var handler = new CancelledHandler(); // 先ほど定義したキャンセル対応のモック
        var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://dummyAppId/k/v1/");

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        // テスト用ファイルストリーム（中身は不要）
        var dummyFileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileName = "test.txt";

        cts.Cancel(); // キャンセルを事前にトリガー

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => {
            await api.UploadFileAsync(dummyFileStream, fileName, cts.Token);
        });
    }

    /// <summary>
    /// UploadFileAsync メソッドが HTTP クライアントのタイムアウトによりキャンセルされた場合に、TaskCanceledException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncTimeoutThrowsTaskCanceledException() {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken token) => {
                await Task.Delay(TimeSpan.FromSeconds(5), token); // 意図的な遅延
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent("{\"fileKey\": \"dummyKey\"}")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object) {
            Timeout = TimeSpan.FromMilliseconds(100) // タイムアウトを極端に短く
        };
        httpClient.BaseAddress = new Uri("https://dummyAppId/k/v1/");

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        var dummyContent = new MemoryStream(Encoding.UTF8.GetBytes("dummy data"));
        var fileName = "test.txt";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TaskCanceledException>(async () => {
            await api.UploadFileAsync(dummyContent, fileName);
        });

        Assert.True(ex is not null, "Expected TaskCanceledException due to timeout");
    }

    /// <summary>
    /// UploadFileAsync メソッドが API から予期しない Content-Type（例：text/html）でレスポンスを受け取った場合に、KintoneException をスローすることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncResponseWithUnexpectedContentTypeThrowsJsonException() {
        // Arrange
        var unexpectedContent = "<html><body>Service Unavailable</body></html>";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(unexpectedContent, Encoding.UTF8, "text/html")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        httpClient.BaseAddress = new Uri("https://dummyAppId/k/v1/");
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        var dummyContent = new MemoryStream(Encoding.UTF8.GetBytes("dummy data"));
        var fileName = "test.txt";

        // Act & Assert
        await Assert.ThrowsAsync<KintoneException>(async () => {
            await api.UploadFileAsync(dummyContent, fileName);
        });
    }

    /// <summary>
    /// UploadFileAsync メソッドが読み込み中に例外をスローするストリームを受け取った場合に、HttpRequestException をスローし、その内部例外が IOException であることを検証するテスト。
    /// </summary>
    [Fact]
    public async Task UploadFileAsyncStreamThrowsExceptionDuringReadThrowsHttpRequestException() {
        // Arrange
        var dummyData = Encoding.UTF8.GetBytes(new string('A', 1024));
        var throwingStream = new ThrowingStream(dummyData, throwAfterBytes: 512);

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) => {
                var response = new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StreamContent(throwingStream)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return response;
            });

        var httpClient = new HttpClient(handlerMock.Object);
        httpClient.BaseAddress = new Uri("https://dummyAppId/k/v1/");
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var api = new KintoneApi(new ApiTokenAccess("dummyAppId", "dummyToken"), 123, factory.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => {
            await api.UploadFileAsync(throwingStream, "faulty_file.txt", CancellationToken.None);
        });

        // 内部例外がIOExceptionか確認する
        Assert.NotNull(ex.InnerException);
        Assert.IsType<IOException>(ex.InnerException);
        Assert.Contains("読み込み中に例外", ex.InnerException.Message); // ThrowingStream の例外メッセージに合わせて
    }

    #region <<Private methods>>
    /// <summary>
    /// Content-Disposition ヘッダーを抽出するための正規表現を生成するメソッド。
    /// </summary>
    /// <returns>Content-Disposition ヘッダーを抽出する正規表現オブジェクト</returns>
    [GeneratedRegex(@"Content-Disposition: form-data;[^\r\n]*")]
    private static partial Regex ContentDispositionRegex();

    /// <summary>
    /// Content-Disposition ヘッダーから filename= の部分を抽出するための正規表現を生成するメソッド。
    /// </summary>
    /// <returns>filename= の部分を抽出する正規表現オブジェクト</returns>
    [GeneratedRegex(@"filename=(?:""([^""]*)""|([^;]*))")]
    private static partial Regex FileNameRegex();
    #endregion
}
