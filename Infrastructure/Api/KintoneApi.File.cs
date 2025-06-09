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
    public async Task<string> UploadFileAsync(Stream stream, string fileName) {
        return await UploadFileInternalAsync(stream, fileName, CancellationToken.None);
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, CancellationToken cancellationToken) {
        return await UploadFileInternalAsync(stream, fileName, cancellationToken);
    }

    private async Task<string> UploadFileInternalAsync(Stream stream, string fileName, CancellationToken cancellationToken) {
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

        if (stream.Length > this.MaxUploadFileSize) {
            var message = $"ファイルサイズが制限（{this.MaxUploadFileSize / 1024 / 1024}MB）を超えています。: {stream.Length} bytes";
            _logger?.LogError(message);
            throw new KintoneException(new KintoneError {
                Code = "LOCAL_FILE_TOO_LARGE",
                Message = message,
                Summary = $"アップロードファイルサイズがKintoneの上限（{this.MaxUploadFileSize}MB）を超えています。"
            });
        }

        var safeFileName = Path.GetFileName(fileName).Trim();
        if (string.IsNullOrEmpty(safeFileName)) {
            safeFileName = "unnamed_file";
        }

        stream.Position = 0;

        var content = new MultipartFormDataContent();
        var fileCnt = new StreamContent(stream);
        fileCnt.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileCnt, "file", safeFileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "file.json");
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);
        request.Content = content;

        using var resp = await this._httpClient.SendAsync(request, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode) {
            var error = KintoneErrorConverter.Parse(json);
            _logger?.LogError("Kintoneファイルアップロード失敗: {Error}", error);
            throw new KintoneException(error);
        }

        // Content-Type チェック（補強処理）
        if (!resp.Content.Headers.ContentType?.MediaType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) ?? true) {
            var mediaType = resp.Content.Headers.ContentType?.MediaType ?? "null";
            var errorMessage = $"想定外の Content-Type: {mediaType}";
            _logger?.LogError(errorMessage);
            throw new KintoneException(new KintoneError {
                Code = "INVALID_CONTENT_TYPE",
                Message = errorMessage,
                Summary = "Kintone API からのレスポンス形式が不正です。"
            });
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
        ArgumentNullException.ThrowIfNull(fileKey);
        if (fileKey == string.Empty) {
            throw new ArgumentException("fileKey must not be empty.");
        }

        var url = $"file.json?fileKey={Uri.EscapeDataString(fileKey)}";
        try {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

            using var resp = await this._httpClient.SendAsync(request);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode) {
                // application/json の場合は Kintone のエラーメッセージをパースする
                if (resp.Content.Headers.ContentType?.MediaType == "application/json") {
                    var error = KintoneErrorConverter.Parse(body);
                    throw new KintoneException(error);
                }

                throw new KintoneException($"ファイル取得に失敗しました。Status: {resp.StatusCode} Raw response: {body}");
            }

            // 成功時は Content-Type に関わらずバイト列として返す
            return await resp.Content.ReadAsByteArrayAsync();
        } catch (TaskCanceledException ex) {
            throw new KintoneException("HTTPリクエストがタイムアウトしました。", ex);
        } catch (HttpRequestException ex) {
            throw new KintoneException("HTTPリクエストに失敗しました。", ex);
        }
    }

    /// <summary>
    /// fileKey からファイルをダウンロードし、ストリームとして返します。
    /// </summary>
    public async Task<Stream> DownloadFileStreamAsync(string fileKey) {
        ArgumentNullException.ThrowIfNull(fileKey);
        if (fileKey == string.Empty) {
            throw new ArgumentException("fileKey must not be empty.");
        }
        var url = $"file.json?fileKey={Uri.EscapeDataString(fileKey)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Cybozu-API-Token", this.ApiToken);

        var resp = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        var contentType = resp.Content.Headers.ContentType?.MediaType;

        if (!resp.IsSuccessStatusCode || contentType != "application/octet-stream") {
            var json = await resp.Content.ReadAsStringAsync();

            KintoneError? error = null;
            try {
                error = KintoneErrorConverter.Parse(json);
            } catch (Exception ex) {
                _logger?.LogError(ex, "Kintoneエラー解析失敗: fileKey={FileKey}, body={Json}", fileKey, json);
                throw new KintoneException($"予期しないContent-Type: {contentType}, body={json}");
            }

            if (error != null) {
                _logger?.LogError("Kintoneファイルダウンロード失敗（ストリーム）: fileKey={FileKey}, Error={Error}", fileKey, error);
                throw new KintoneException(error);
            }

            throw new KintoneException($"予期しないContent-Type: {contentType}, body={json}");

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
