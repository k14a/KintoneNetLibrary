using System.Net.Http.Headers;

namespace KintoneNetLibrary.Tests.Helpers;

internal static class HttpRequestMessageExtensions {
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
