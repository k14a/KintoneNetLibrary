using System.Text.Json;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Backup.Application.DTOs;
using KintoneNetLibrary.Backup.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Infrastructure.Api;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Backup.Infrastructure.Services;

/// <summary>
/// バックアップサービス
/// </summary>
public sealed class BackupService : IBackupService {
    private readonly BackupOptions _options;
    private readonly IKintoneApi _api;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ILogger<BackupService>? _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="options"></param>
    /// <param name="schemaProvider"></param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public BackupService(
        BackupOptions options,
        ISchemaProvider schemaProvider,
        HttpClient? httpClient = null,
        ILogger<BackupService>? logger = null) {

        ArgumentNullException.ThrowIfNull(options);

        this._options = options;
        this._schemaProvider = schemaProvider;
        this._logger = logger;

        var access = new ApiTokenAccess(options.SubDomain, options.ApiToken);

        httpClient ??= new HttpClient {
            BaseAddress = new Uri($"https://{options.SubDomain}/k/v1/")
        };

        // KintoneApi の初期化
        this._api = new KintoneApi(
            access: access,
            appID: options.AppID,
            httpClient: httpClient
        );

        // BatchSize が指定されていれば KintoneApi に反映
        if (options.BatchSize is int size) {
            this._api.CursorPageSize = size; // KintoneApi 側でバリデーション
        }
    }

    /// <summary>
    /// バックアップを実行します
    /// </summary>
    public async Task<BackupResult> RunBackupAsync() {
        var result = new BackupResult();

        try {
            this._logger?.LogInformation("バックアップ開始: App={App}", this._options.AppID);

            // 1) レコード取得
            var json = await this.FetchRecordsAsync();

            // 2) JSON 保存
            var jsonResult = await this.SaveJsonAsync(json);
            if (jsonResult) {
                result.JsonSaved = true;
            } else {
                // 上書き禁止などのスキップ
                var message = "JSON 保存がスキップされました";
                this._logger?.LogWarning("{message}", message);
                result.JsonSaved = false;
                result.Skipped.Add(message);
            }

            // 3) フィールドスキーマ保存
            var schemaResult = await this.SaveFieldSchemaAsync();
            if (schemaResult) {
                result.SchemaSaved = true;
            } else {
                // 上書き禁止などのスキップ
                var message = "フィールドスキーマ保存がスキップされました";
                this._logger?.LogWarning("{message}", message);
                result.SchemaSaved = false;
                result.Skipped.Add(message);
            }

            // 4) 添付ファイルダウンロード
            if (this._options.DownloadFiles) {
                var (success, fail) = await this.DownloadFilesWithResultAsync(json);
                result.FileDownloadedCount = success;
                result.FileFailedCount = fail;

                if (fail > 0) {
                    result.Warnings.Add($"{fail} 件の添付ファイルのダウンロードに失敗しました");
                }
            }

            this._logger?.LogInformation("バックアップ完了");

        } catch (Exception ex) {
            this._logger?.LogError(ex, "バックアップ中に致命的エラーが発生しました");
            result.Success = false;
            result.ErrorMessages.Add(ex.Message);

        } finally {
            // 成功判定（致命的エラーがなければ true）
            if (result.Success) {
                // ファイル失敗があれば部分成功
                result.Success = result.FileFailedCount == 0;
            }

            this._logger?.LogInformation("バックアップ処理が終了しました");
        }

        return result;
    }

    /// <summary>
    /// レコードを取得します
    /// </summary>
    /// <returns></returns>
    private async Task<string> FetchRecordsAsync() {
        if (!string.IsNullOrWhiteSpace(this._options.Query)) {
            return await this._api.RawFindByQueryAsync(
                this._options.Query!,
                fieldCodes: this._options.FieldCodes
            ) ?? "{}";
        }

        return await this._api.RawFindAllAsync(
            fieldCodes: this._options.FieldCodes
        ) ?? "{}";
    }

    /// <summary>
    /// JSON を保存します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    private async Task<bool> SaveJsonAsync(string json) {
        Directory.CreateDirectory(Path.GetDirectoryName(this._options.OutputPath)!);

        if (File.Exists(this._options.OutputPath) && !this._options.Overwrite) {
            this._logger?.LogWarning("出力先ファイルが既に存在するためスキップされました: {Path}", this._options.OutputPath);
            return false;
        }

        await File.WriteAllTextAsync(this._options.OutputPath, json);
        this._logger?.LogInformation("JSON を保存しました: {Path}", this._options.OutputPath);
        return true;
    }

    private async Task<(int success, int fail)> DownloadFilesWithResultAsync(string json) {
        int success = 0;
        int fail = 0;

        this._logger?.LogInformation("添付ファイルのダウンロードを開始します");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("records", out var records)) {
            this._logger?.LogWarning("records が見つからないため、添付ファイルのダウンロードをスキップします");
            return (0, 0);
        }

        var filesRoot = Path.Combine(Path.GetDirectoryName(this._options.OutputPath)!, "files");
        Directory.CreateDirectory(filesRoot);

        foreach (var record in records.EnumerateArray()) {
            var recordId = record.TryGetProperty("レコード番号", out var idField)
                ? idField.GetProperty("value").GetString()
                : Guid.NewGuid().ToString();

            var recordDir = Path.Combine(filesRoot, recordId!);
            Directory.CreateDirectory(recordDir);

            foreach (var field in record.EnumerateObject()) {
                if (field.Value.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "FILE") {
                    var fileArray = field.Value.GetProperty("value");

                    foreach (var fileInfo in fileArray.EnumerateArray()) {
                        var fileKey = fileInfo.GetProperty("fileKey").GetString();
                        var fileName = fileInfo.GetProperty("name").GetString();

                        if (string.IsNullOrEmpty(fileKey) || string.IsNullOrEmpty(fileName)) {
                            continue;
                        }

                        var savePath = Path.Combine(recordDir, fileName);

                        try {
                            var bytes = await this._api.DownloadFileAsync(fileKey);
                            await File.WriteAllBytesAsync(savePath, bytes);

                            success++;
                            this._logger?.LogInformation("Saved: {Path}", savePath);

                        } catch (Exception ex) {
                            fail++;
                            this._logger?.LogError(ex, "ファイルのダウンロードに失敗しました: fileKey={FileKey}", fileKey);
                        }
                    }
                }
            }
        }

        this._logger?.LogInformation("添付ファイルのダウンロードが完了しました");

        return (success, fail);
    }

    /// <summary>
    /// フィールドスキーマを保存します
    /// </summary>
    /// <returns></returns>
    private async Task<bool> SaveFieldSchemaAsync() {
        if (!this._options.IncludeFieldSchema) {
            this._logger?.LogInformation("フィールドスキーマのバックアップはスキップされました");
            return false;
        }

        this._logger?.LogInformation("フィールドスキーマを取得しています…");

        // 1. スキーマ取得
        var metadata = await this._schemaProvider.GetMetadataAsync(this._options.AppID, this._options.ApiToken);

        // 2. JSON シリアライズ
        var json = JsonSerializer.Serialize(
            metadata,
            new JsonSerializerOptions {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }
        );

        // 3. 保存先パス
        var dir = Path.GetDirectoryName(this._options.OutputPath)!;
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, this._options.FieldSchemaFileName);

        // 4. 上書きチェック
        if (File.Exists(filePath) && !this._options.Overwrite) {
            this._logger?.LogWarning("fields.json が既に存在するためスキップされました: {Path}", filePath);
            return false;
        }

        try {
            // 5. 書き込み
            await File.WriteAllTextAsync(filePath, json);
            this._logger?.LogInformation("フィールドスキーマを保存しました: {Path}", filePath);
            return true;

        } catch (Exception ex) {
            this._logger?.LogError(ex, "フィールドスキーマの取得に失敗しました");
            // throw;
            return false;
        }
    }

}
