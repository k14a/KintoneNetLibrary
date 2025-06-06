using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {
    /* =========================================================
       ファイルアップロード
       ========================================================= */

    /// <summary>
    /// 任意のストリームを kintone にアップロードし、fileKey を返します。
    /// </summary>
    // public async Task<string> UploadFileAsync(Stream stream, string fileName) {
    public async Task<string> UploadFileAsync(Stream stream, string fileName) {
        ArgumentNullException.ThrowIfNull(stream);
        fileName ??= "";
        if (fileName.Any(char.IsControl)) {
            _logger?.LogWarning("ファイル名に制御文字が含まれていたため、除去されました: {Original}", fileName);
            fileName = string.Concat(fileName.Where(c => !char.IsControl(c)));
        }


        if (!stream.CanSeek) {
            throw new InvalidOperationException("アップロード前にファイルサイズを確認できるよう、シーク可能なストリームを使用してください。");
        }

        if (stream.Length == 0) {
            throw new InvalidOperationException("空のファイルはアップロードできません。");
        }

        // 容量制限チェック
        if (stream.Length > this.MaxUploadFileSize) {
            var message = $"ファイルサイズが制限（{this.MaxUploadFileSize / 1024 / 1024}MB）を超えています。: {stream.Length} bytes";
            _logger?.LogError(message);
            throw new KintoneException(new KintoneError {
                Code = "LOCAL_FILE_TOO_LARGE",
                Message = message,
                Summary = $"アップロードファイルサイズがKintoneの上限（{this.MaxUploadFileSize}MB）を超えています。"
            });
        }

        // null や空白文字のみのファイル名は代替ファイル名に置き換え
        var safeFileName = Path.GetFileName(fileName).Trim();
        if (string.IsNullOrEmpty(safeFileName)) {
            safeFileName = "unnamed_file";
        }

        stream.Position = 0; // 念のため先頭に戻す

        var content = new MultipartFormDataContent();
        var fileCnt = new StreamContent(stream);
        fileCnt.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileCnt, "file", safeFileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "file.json");
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        request.Content = content;

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            var error = KintoneErrorConverter.Parse(json);
            _logger?.LogError("Kintoneファイルアップロード失敗: {Error}", error);
            throw new KintoneException(error);
        }

        var result = JsonSerializer.Deserialize<FileUploadResult>(json, _jsonOptions) ?? new FileUploadResult();

        if (string.IsNullOrEmpty(result.FileKey)) {
            throw new KintoneException(new KintoneError {
                Code = "FILEKEY_MISSING",
                Message = "レスポンスに fileKey が存在しません。",
                Summary = "アップロード処理のレスポンスに fileKey が含まれていません。"
            });
        }

        return result.FileKey;
    }

    /* =========================================================
       ファイルダウンロード
       ========================================================= */

    public async Task<byte[]> DownloadFileAsync(string fileKey) {
        var url = $"file.json?fileKey={Uri.EscapeDataString(fileKey)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

        using var resp = await this._httpClient.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode) {
            var error = KintoneErrorConverter.Parse(json);
            _logger?.LogError("Kintoneファイルダウンロード失敗（バイト配列）: fileKey={FileKey}, Error={Error}", fileKey, error);
            throw new KintoneException(error);
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

        var resp = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!resp.IsSuccessStatusCode) {
            var json = await resp.Content.ReadAsStringAsync();
            var error = KintoneErrorConverter.Parse(json);
            _logger?.LogError("Kintoneファイルダウンロード失敗（ストリーム）: fileKey={FileKey}, Error={Error}", fileKey, error);
            throw new KintoneException(error);
        }

        return await resp.Content.ReadAsStreamAsync();
    }

    /* =========================================================
       内部 DTO
       ========================================================= */
    private sealed class FileUploadResult {
        [JsonPropertyName("fileKey")]
        public string FileKey { get; set; } = string.Empty;
    }
}
