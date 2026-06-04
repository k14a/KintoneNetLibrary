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
    /// <summary>
    /// HttpRequestMessage を受け取り、HttpResponseMessage を返すハンドラー関数を指定して、モックされた HttpClient を作成します。
    /// </summary>
    /// <param name="handler">HttpRequestMessage を受け取り、HttpResponseMessage を返す関数</param>
    /// <returns>モックされた HttpClient</returns>
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

    /// <summary>
    /// HttpRequestMessage を完全に複製するための拡張メソッド。これにより、元のリクエストが破棄されたり、ストリームが閉じられたりしても、テストで安全に使用できるようになります。
    /// </summary>
    /// <param name="request">複製する HttpRequestMessage</param>
    /// <returns>複製された HttpRequestMessage</returns>
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
                if (fileName != null) {
                    newMultipart.Add(newContent, name!, fileName);
                } else {
                    newMultipart.Add(newContent, name!);
                }
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

/// <summary>
/// リクエストがキャンセルされた場合のハンドラー。キャンセルトークンがキャンセルされていると例外をスローします。
/// </summary>
public class CancelledHandler : HttpMessageHandler {
    /// <summary>
    /// リクエストがキャンセルされた場合、OperationCanceledException をスローします。
    /// </summary>
    /// <param name="request">送信される HTTP リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>HTTP レスポンスメッセージ</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}

/// <summary>
/// リクエストがタイムアウトした場合のハンドラー。指定された時間だけ待機し、その後に HTTP 200 OK を返します。
/// </summary>
public class TimeoutHandler : HttpMessageHandler {
    /// <summary>
    /// リクエストがタイムアウトした場合、指定された時間だけ待機し、その後に HTTP 200 OK を返します。
    /// </summary>
    /// <param name="request">送信される HTTP リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>HTTP レスポンスメッセージ</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken); // 故意に長時間待機
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

/// <summary>
/// レスポンスの Content-Type が想定外の場合のハンドラー。HTTP 200 OK を返しますが、Content-Type は "text/html" として設定されます。
/// </summary>
public class UnexpectedContentTypeHandler : HttpMessageHandler {
    /// <summary>
    /// レスポンスの Content-Type が想定外の場合、HTTP 200 OK を返しますが、Content-Type は "text/html" として設定されます。
    /// </summary>
    /// <param name="request">送信される HTTP リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>HTTP レスポンスメッセージ</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("<html><body>Error</body></html>")
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html"); // 想定外
        return Task.FromResult(response);
    }
}

/// <summary>
/// レスポンスのストリームが途中で切断される場合のハンドラー。HTTP 200 OK を返しますが、Content のストリームは指定されたバイト数で切断されます。
/// </summary>
public class StreamCutoffHandler : HttpMessageHandler {
    /// <summary>
    /// レスポンスのストリームが途中で切断される場合、HTTP 200 OK を返しますが、Content のストリームは指定されたバイト数で切断されます。
    /// </summary>
    /// <param name="request">送信される HTTP リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>HTTP レスポンスメッセージ</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var faultyStream = new CutoffStream(new byte[] { 1, 2, 3, 4, 5 }, cutoffAfterBytes: 3);
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StreamContent(faultyStream)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); // 必要に応じて

        return Task.FromResult(response);
    }
}
