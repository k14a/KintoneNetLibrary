using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Infrastructure.Api;

// コメントは日本語で記述
/// <summary>
/// Kintone API のファイル操作に関する機能を提供します。
/// </summary>
public partial class KintoneApi {
    #region <<File upload>>
    /// <summary>
    /// 任意のストリームを kintone にアップロードし、fileKey を返します。
    /// </summary>
    public async Task<string> UploadFileAsync(Stream stream, string fileName) {
        return await this.UploadFileInternalAsync(stream, fileName, CancellationToken.None);
    }
    /// <summary>
    /// 任意のストリームを kintone にアップロードし、fileKey を返します。
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="fileName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> UploadFileAsync(Stream stream, string fileName, CancellationToken cancellationToken) {
        return await this.UploadFileInternalAsync(stream, fileName, cancellationToken);
    }
    /// <summary>
    /// 指定されたファイルを kintone にアップロードし、fileKey を返します。
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<string> UploadFileAsync(FileInfo file) {
        if (!file.Exists) {
            throw new FileNotFoundException("指定されたファイルが存在しません。", file.FullName);
        }

        using var stream = file.OpenRead();
        return await this.UploadFileAsync(stream, file.Name);
    }
    /// <summary>
    /// 指定されたファイルを kintone にアップロードし、fileKey を返します。
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="fileName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="KintoneException"></exception>
    private async Task<string> UploadFileInternalAsync(Stream stream, string fileName, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(stream);
        fileName ??= "";

        if (fileName.Any(char.IsControl)) {
            this._logger?.LogWarning("ファイル名に制御文字が含まれていたため、除去されました: {Original}", fileName);
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
            this._logger?.LogError(message);
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
        request.Headers.Add("X-Cybozu-API-Token", this._access.ApiToken);
        request.Content = content;

        using var resp = await this._httpClient.SendAsync(request, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode) {
            var error = KintoneErrorConverter.Parse(json);
            this._logger?.LogError("Kintoneファイルアップロード失敗: {Error}", error);
            throw new KintoneException(error);
        }

        // Content-Type チェック（補強処理）
        if (!resp.Content.Headers.ContentType?.MediaType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) ?? true) {
            var mediaType = resp.Content.Headers.ContentType?.MediaType ?? "null";
            var errorMessage = $"想定外の Content-Type: {mediaType}";
            this._logger?.LogError(errorMessage);
            throw new KintoneException(new KintoneError {
                Code = "INVALID_CONTENT_TYPE",
                Message = errorMessage,
                Summary = "Kintone API からのレスポンス形式が不正です。"
            });
        }

        var result = JsonSerializer.Deserialize<FileUploadResult>(json, this._jsonOptions) ?? new FileUploadResult();

        if (string.IsNullOrEmpty(result.FileKey)) {
            throw new KintoneException(new KintoneError {
                Code = "FILEKEY_MISSING",
                Message = "レスポンスに fileKey が存在しません。",
                Summary = "アップロード処理のレスポンスに fileKey が含まれていません。"
            });
        }

        return result.FileKey;
    }
    #endregion

    #region <<File upload>>
    /// <summary>
    /// fileKey からファイルをダウンロードし、バイト配列として返します。
    /// </summary>
    /// <param name="fileKey"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<byte[]> DownloadFileAsync(string fileKey) {
        ArgumentNullException.ThrowIfNull(fileKey);
        if (fileKey == string.Empty) {
            throw new ArgumentException("fileKey must not be empty.");
        }

        var url = $"file.json?fileKey={Uri.EscapeDataString(fileKey)}";
        try {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Cybozu-API-Token", this._access.ApiToken);

            using var resp = await this._httpClient.SendAsync(request);
            var contentType = resp.Content.Headers.ContentType?.MediaType;
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode) {
                if (contentType == "application/json") {
                    // 成功ステータスでもapplication/jsonが返ってきた場合はKintoneのエラーとみなす
                    var error = KintoneErrorConverter.Parse(body);
                    throw new KintoneException(error);
                }

                if (!MimeTypeWrapper.IsAcceptableContentType(contentType)) {
                    throw new KintoneException($"予期しないContent-Typeが返されました。Content-Type: {contentType} Response: {body}");
                }
                // if (contentType != "application/octet-stream") {
                //     throw new KintoneException($"予期しないContent-Typeが返されました。Content-Type: {contentType} Response: {body}");
                // }

                // 成功時は Content-Type に関わらずバイト列として返す
                return await resp.Content.ReadAsByteArrayAsync();

            } else {
                // throw new KintoneException($"ファイル取得に失敗しました。Status: {resp.StatusCode} Raw response: {body}");
                var error = KintoneErrorConverter.Parse(body);
                throw new KintoneException(error);
            }

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
        request.Headers.Add("X-Cybozu-API-Token", this._access.ApiToken);

        var resp = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        var contentType = resp.Content.Headers.ContentType?.MediaType;

        if (!resp.IsSuccessStatusCode || contentType != "application/octet-stream") {
            var json = await resp.Content.ReadAsStringAsync();

            KintoneError? error = null;
            try {
                error = KintoneErrorConverter.Parse(json);
            } catch (Exception ex) {
                this._logger?.LogError(ex, "Kintoneエラー解析失敗: fileKey={FileKey}, body={Json}", fileKey, json);
                throw new KintoneException($"予期しないContent-Type: {contentType}, body={json}");
            }

            if (error != null) {
                this._logger?.LogError("Kintoneファイルダウンロード失敗（ストリーム）: fileKey={FileKey}, Error={Error}", fileKey, error);
                throw new KintoneException(error);
            }

            throw new KintoneException($"予期しないContent-Type: {contentType}, body={json}");

        }

        return await resp.Content.ReadAsStreamAsync();
    }
    /// <summary>
    /// fileKey からファイルをダウンロードし、指定されたファイルに保存します。
    /// </summary>
    /// <param name="fileKey"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public async Task DownloadFileAsync(string fileKey, FileInfo destination) {
        var bytes = await this.DownloadFileAsync(fileKey);
        using var fs = destination.OpenWrite();
        await fs.WriteAsync(bytes, 0, bytes.Length);
    }
    #endregion

    /// <summary>
    /// ファイルアップロード結果
    /// </summary>
    private sealed class FileUploadResult {
        /// <summary>
        /// アップロードされたファイルの fileKey
        /// </summary>
        [JsonPropertyName("fileKey")]
        public string FileKey { get; set; } = string.Empty;
    }
}
