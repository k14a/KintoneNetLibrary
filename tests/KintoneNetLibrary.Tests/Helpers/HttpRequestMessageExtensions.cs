using System.Net.Http.Headers;

namespace KintoneNetLibrary.Tests.Helpers;

/// <summary>
/// HttpRequestMessageとHttpResponseMessageの内容を完全に複製するための拡張メソッド。
/// </summary>
internal static class HttpRequestMessageExtensions {
    /// <summary>
    /// HttpRequestMessageを完全に複製します。Contentも複製されるため、元のHttpRequestMessageの内容を変更してもクローンには影響しません。
    /// </summary>
    /// <param name="request">複製するHttpRequestMessage</param>
    /// <returns>複製されたHttpRequestMessage</returns>
    public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request) {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) {
            Version = request.Version
        };

        foreach (var header in request.Headers) {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null) {
            clone.Content = await request.Content.CloneAsync();
        }

        return clone;
    }

    /// <summary>
    /// HttpResponseMessageを完全に複製します。Contentも複製されるため、元のHttpResponseMessageの内容を変更してもクローンには影響しません。
    /// </summary>
    /// <param name="response">複製するHttpResponseMessage</param>
    /// <returns>複製されたHttpResponseMessage</returns>
    public static async Task<HttpResponseMessage> CloneAsync(this HttpResponseMessage response) {
        var clone = new HttpResponseMessage(response.StatusCode) {
            ReasonPhrase = response.ReasonPhrase,
            Version = response.Version,
            RequestMessage = response.RequestMessage, // 必要に応じてCloneAsyncも検討
        };

        foreach (var header in response.Headers) {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (response.Content != null) {
            clone.Content = await response.Content.CloneAsync();
        }

        return clone;
    }

    /// <summary>
    /// HttpContentを完全に複製します。MultipartFormDataContentの場合は各パートも複製されます。
    /// </summary>
    /// <param name="content">複製するHttpContent</param>
    /// <returns>複製されたHttpContent</returns>
    public static async Task<HttpContent> CloneAsync(this HttpContent content) {
        if (content == null) {
            return null!;
        }

        if (content is MultipartFormDataContent multipart) {
            var newMultipart = new MultipartFormDataContent();

            foreach (var part in multipart) {
                var partClone = await part.CloneAsync();
                var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                var fileName = part.Headers.ContentDisposition?.FileName?.Trim('"');

                newMultipart.Add(partClone, name!, fileName);
            }

            foreach (var header in multipart.Headers) {
                newMultipart.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return newMultipart;
        }

        // その他の HttpContent はバイト配列として複製
        var data = await content.ReadAsByteArrayAsync();
        var newContent = new ByteArrayContent(data);

        foreach (var header in content.Headers) {
            newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return newContent;
    }
}
