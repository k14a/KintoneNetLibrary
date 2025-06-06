using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// HttpClient をモックするためのユーティリティクラス。
/// </summary>
public static class KintoneHttpTestHelper {
    public static HttpClient CreateMockHttpClient(Func<HttpRequestMessage, HttpResponseMessage> handler) {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, token) => {
                // request を事前に完全複製し、破棄の影響を受けないようにする
                var clonedRequest = await request.CloneAsync();
                return handler(clonedRequest);
            });

        return new HttpClient(mockHandler.Object) {
            BaseAddress = new Uri("https://dummyappid/k/v1/")
        };
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request) {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) {
            Version = request.Version
        };

        // ヘッダーのコピー
        foreach (var header in request.Headers) {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // コンテンツのコピー
        if (request.Content is MultipartFormDataContent multipart) {
            var newMultipart = new MultipartFormDataContent();

            foreach (var content in multipart) {
                var data = await content.ReadAsByteArrayAsync();
                var newContent = new ByteArrayContent(data);

                // ヘッダーコピー
                foreach (var header in content.Headers) {
                    newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var name = content.Headers.ContentDisposition?.Name?.Trim('"');
                var fileName = content.Headers.ContentDisposition?.FileName?.Trim('"');
                newMultipart.Add(newContent, name!, fileName);
            }

            foreach (var header in multipart.Headers) {
                newMultipart.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = newMultipart;
        } else if (request.Content != null) {
            var contentData = await request.Content.ReadAsByteArrayAsync();
            var newContent = new ByteArrayContent(contentData);

            foreach (var header in request.Content.Headers) {
                newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = newContent;
        }

        return clone;
    }

}
public class CancelledHandler : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
public class TimeoutHandler : HttpMessageHandler {
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken); // 故意に長時間待機
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
public class UnexpectedContentTypeHandler : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("<html><body>Error</body></html>")
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html"); // 想定外
        return Task.FromResult(response);
    }
}
public class StreamCutoffHandler : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var faultyStream = new CutoffStream(new byte[] { 1, 2, 3, 4, 5 }, cutoffAfterBytes: 3);
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StreamContent(faultyStream)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); // 必要に応じて

        return Task.FromResult(response);
    }
}
