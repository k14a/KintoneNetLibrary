using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Types;

namespace KintoneNetLibrary.Api;

public partial class KintoneApi
{
    /* =========================================================
       ファイルアップロード
       ========================================================= */

    /// <summary>
    /// 任意のストリームを kintone にアップロードし、fileKey を返します。
    /// </summary>
    public async Task<string> UploadFileAsync(Stream stream, string fileName) {
        var content = new MultipartFormDataContent();
        var fileCnt = new StreamContent(stream);
        fileCnt.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileCnt, "\"file\"", $"\"{Path.GetFileName(fileName)}\"");

        using var request = new HttpRequestMessage(HttpMethod.Post, "file.json");
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        request.Content = content;

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var result = JsonSerializer.Deserialize<FileUploadResult>(json, _jsonOptions) ?? new FileUploadResult();

        return result.FileKey;
    }

    /* =========================================================
       ファイルダウンロード
       ========================================================= */

    /// <summary>
    /// fileKey からファイルをダウンロードし、byte[] を返します。
    /// </summary>
    public async Task<byte[]> DownloadFileAsync(string fileKey) {
        var url = $"file.json?fileKey={Uri.EscapeDataString(fileKey)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

        using var resp = await this._httpClient.SendAsync(request);
        if (!resp.IsSuccessStatusCode) {
            var json = await resp.Content.ReadAsStringAsync();
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return await resp.Content.ReadAsByteArrayAsync();
    }

    /// <summary>
    /// fileKey からファイルをダウンロードし、ストリームとして返します。
    /// </summary>
    public async Task<Stream> DownloadFileStreamAsync(string fileKey) {
        var url = $"file.json?fileKey={Uri.EscapeDataString(fileKey)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

        var resp = await this._httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead);

        if (!resp.IsSuccessStatusCode) {
            var json = await resp.Content.ReadAsStringAsync();
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return await resp.Content.ReadAsStreamAsync();
    }

    /* =========================================================
       内部 DTO
       ========================================================= */
    private sealed class FileUploadResult
    {
        [JsonPropertyName("fileKey")]
        public string FileKey { get; set; } = string.Empty;
    }
}
