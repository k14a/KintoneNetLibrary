using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Backup.Application.DTOs;
using KintoneNetLibrary.Backup.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Enums;
using KintoneNetLibrary.Infrastructure.Api;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Backup.Infrastructure.Services;

/// <summary>
/// バックアップサービス
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
/// <param name="schemaProvider"></param>
/// <param name="httpClient"></param>
/// <param name="logger"></param>
public sealed class BackupService(ISchemaProvider schemaProvider, HttpClient? httpClient = null, ILogger<BackupService>? logger = null) : IBackupService {
    private IKintoneApi? _api;
    private readonly ISchemaProvider _schemaProvider = schemaProvider;
    private HttpClient? _httpClient = httpClient;
    private JsonSerializerOptions? _jsonOptions;
    private readonly ILogger<BackupService>? _logger = logger;
    public BackupOptions Options { get; set; } = default!;

    private void EnsureApiInitialized() {
        if (this._api != null) { return; }
        ArgumentNullException.ThrowIfNull(this.Options);

        var domain = $"{this.Options.SubDomain}.cybozu.com";
        var access = new ApiTokenAccess(domain, this.Options.ApiToken);

        this._httpClient ??= new HttpClient {
            BaseAddress = new Uri($"https://{domain}/k/v1/")
        };

        this._jsonOptions ??= new JsonSerializerOptions() {
            WriteIndented = this.Options.Pretty,
            Encoder = this.Options.EscapeUnicode
                ? null
                : System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        this._api = new KintoneApi(access: access, appID: this.Options.AppID, httpClient: this._httpClient, jsonOptions: this._jsonOptions);

        // BatchSize が指定されていれば KintoneApi に反映
        if (this.Options.BatchSize is int size) {
            this._api.CursorPageSize = size; // KintoneApi 側でバリデーション
        }
    }

    /// <summary>
    /// バックアップを実行します
    /// </summary>
    public async Task<BackupResult> RunBackupAsync() {
        this._logger?.LogInformation("バックアップ 開始: App={App}", this.Options.AppID);

        this.EnsureApiInitialized();
        var result = new BackupResult();

        try {
            this._logger?.LogInformation("バックアップ開始: App={App}", this.Options.AppID);

            // 0) 出力ディレクトリを作成
            var appIdFolder = $"AppID{this.Options.AppID:D6}";
            var timestampFolder = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupRoot = Path.Combine(this.Options.OutputPath.FullName, appIdFolder, timestampFolder);

            // Override == falseの場合は既存チェック
            if (!this.Options.Overwrite && Directory.Exists(backupRoot)) {
                var message = $"バックアップ先ディレクトリが既に存在するため、バックアップを中止しました: {backupRoot}";
                this._logger?.LogError("{message}", message);
                throw new IOException(message);
            }

            // ディレクトリ作成
            Directory.CreateDirectory(backupRoot);
            Directory.CreateDirectory(Path.Combine(backupRoot, "data"));
            Directory.CreateDirectory(Path.Combine(backupRoot, "files"));

            // BackupService内でOutputPathを書き換え
            this.Options.OutputPath = new DirectoryInfo(backupRoot);

            // 1) レコード取得
            var json = await this.FetchRecordsAsync();

            // 1-1) レコード数カウント
            using (var doc = JsonDocument.Parse(json)) {
                var root = doc.RootElement;
                if (root.TryGetProperty("records", out var records)) {
                    result.RecordCount = records.GetArrayLength();
                } else {
                    result.RecordCount = 0;
                }
            }

            // 2) JSON 分割保存
            var parts = await this.SaveSplitJsonFilesAsync(json);
            result.Parts = parts;
            result.SplitSize = this.Options.SplitSize;

            // 3) フィールドスキーマ取得
            this._schemaProvider.SetDomain(this.Options.SubDomain);
            var metadata = await this._schemaProvider.GetMetadataAsync(this.Options.AppID, this.Options.ApiToken);

            // 3-1) スキーマ保存
            var schemaResult = await this.SaveFieldSchemaAsync(metadata);
            result.SchemaSaved = true;

            // 4) 添付ファイルダウンロード
            if (this.Options.DownloadFiles) {
                var hasFileField = metadata.Fields.Any(p => p.FieldType == KintoneFieldType.File);
                if (!hasFileField) {
                    this._logger?.LogInformation("添付ファイルフィールドが存在しないため、ダウンロードをスキップします");
                    result.FileDownloadedCount = 0;
                    result.FileFailedCount = 0;

                } else {
                    var (success, fail) = await this.DownloadFilesWithResultAsync(json);
                    result.FileDownloadedCount = success;
                    result.FileFailedCount = fail;

                    if (fail > 0) {
                        result.Warnings.Add($"{fail} 件の添付ファイルのダウンロードに失敗しました");
                    }
                }
            }

            // 5) マニフェスト保存
            await this.SaveManifestAsync(metadata, result);

            this._logger?.LogInformation("バックアップ完了");

        } catch (Exception ex) {
            this._logger?.LogError(ex, "バックアップ中に致命的エラーが発生しました");
            result.Success = false;
            result.Errors.Add(ex.Message);

        } finally {
            // 成功判定（致命的エラーがなければ true）
            if (result.Success) {
                // ファイル失敗があれば部分成功
                result.Success = result.FileFailedCount == 0;
            }

            this._logger?.LogInformation("バックアップ 完了");
        }

        return result;
    }

    /// <summary>
    /// レコードを取得します
    /// </summary>
    /// <returns></returns>
    private async Task<string> FetchRecordsAsync() {
        if (!string.IsNullOrWhiteSpace(this.Options.Query)) {
            return await this._api!.RawFindByQueryAsync(
                this.Options.Query!,
                fieldCodes: this.Options.FieldCodes
            ) ?? "{}";
        }

        return await this._api!.RawFindAllAsync(
            fieldCodes: this.Options.FieldCodes
        ) ?? "{}";
    }

    /// <summary>
    /// JSON を分割保存します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    private async Task<int> SaveSplitJsonFilesAsync(string json) {
        using var doc = JsonDocument.Parse(json);
        var records = doc.RootElement.GetProperty("records").EnumerateArray().ToList();

        var chunks = records
            .Select((record, index) => new { record, index })
            .GroupBy(x => x.index / this.Options.SplitSize)
            .Select(g => g.Select(x => x.record).ToList())
            .ToList();

        int partIndex = 1;

        foreach (var chunk in chunks) {
            var fileName = $"part-{partIndex:D4}.json";

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions {
                Indented = this.Options.Pretty,
                Encoder = this.Options.EscapeUnicode ? JavaScriptEncoder.Default : JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            })) {
                writer.WriteStartObject();
                writer.WritePropertyName("records");
                writer.WriteStartArray();

                foreach (var record in chunk) {
                    record.WriteTo(writer);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            var jsonText = Encoding.UTF8.GetString(stream.ToArray());
            await this.SaveJsonAsync(jsonText, fileName, "data");

            partIndex++;
        }

        return chunks.Count;
    }

    /// <summary>
    /// JSON を保存します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    private async Task<bool> SaveJsonAsync(string json, string fileName, string? directory = null) {
        if (!this.Options.OutputPath.Exists) {
            this.Options.OutputPath.Create();
        }

        // data ディレクトリを作成
        var dataDir = Path.Combine(this.Options.OutputPath.FullName, directory ?? "");
        Directory.CreateDirectory(dataDir);

        // 保存先パス
        var path = Path.Combine(dataDir, fileName);

        if (File.Exists(path) && !this.Options.Overwrite) {
            this._logger?.LogWarning("出力先ファイルが既に存在するためスキップされました: {Path}", path);
            return false;
        }

        await File.WriteAllTextAsync(path, json);
        this._logger?.LogInformation("JSON を保存しました: {Path}", path);
        return true;
    }

    /// <summary>
    /// 添付ファイルをダウンロードします
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
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

        var filesRoot = Path.Combine(this.Options.OutputPath.FullName, "files");
        Directory.CreateDirectory(filesRoot);

        foreach (var record in records.EnumerateArray()) {
            var recordId = record.TryGetProperty("レコード番号", out var idField)
                ? idField.GetProperty("value").GetString()
                : Guid.NewGuid().ToString();

            var recordDir = Path.Combine(filesRoot, recordId!);
            Directory.CreateDirectory(recordDir);

            foreach (var field in record.EnumerateObject()) {
                if (field.Value.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "FILE") {
                    var fieldCode = field.Name;
                    var fileArray = field.Value.GetProperty("value");

                    var fieldDir = Path.Combine(recordDir, fieldCode);
                    Directory.CreateDirectory(fieldDir);

                    foreach (var fileInfo in fileArray.EnumerateArray()) {
                        var fileKey = fileInfo.GetProperty("fileKey").GetString();
                        var fileName = fileInfo.GetProperty("name").GetString();

                        if (string.IsNullOrEmpty(fileKey) || string.IsNullOrEmpty(fileName)) { continue; }

                        var savePath = Path.Combine(fieldDir, fileName);

                        try {
                            var bytes = await this._api!.DownloadFileAsync(fileKey);
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
    /// マニフェストを保存します
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task SaveManifestAsync(KintoneAppMetadata metadata, BackupResult result) {
        var manifest = new BackupManifest {
            AppId = this.Options.AppID,
            AppRevision = metadata.Revision,
            BackupAt = DateTime.UtcNow,
            RecordCount = result.RecordCount,
            FileFieldCount = metadata.Fields.Count(f => f.FieldType == KintoneFieldType.File),
            FileCount = result.FileDownloadedCount,
            Parts = result.Parts,
            SplitSize = this.Options.SplitSize,
            Options = new {
                this.Options.IncludeFieldSchema,
                this.Options.DownloadFiles,
                this.Options.Overwrite,
                this.Options.Query,
            },
        };
        var path = Path.Combine(this.Options.OutputPath.FullName, "manifest.json");
        if (File.Exists(path) && !this.Options.Overwrite) {
            this._logger?.LogWarning("マニフェストの保存がスキップされました: {Path}", path);
            return;
        }
        this._logger?.LogInformation("マニフェストを保存しています: {Path}", path);
        var json = JsonSerializer.Serialize(manifest, this._jsonOptions);
        await this.SaveJsonAsync(json, path);
    }

    /// <summary>
    /// フィールドスキーマを保存します
    /// </summary>
    /// <returns></returns>
    private async Task<bool> SaveFieldSchemaAsync(KintoneAppMetadata metadata) {
        if (!this.Options.IncludeFieldSchema) {
            this._logger?.LogInformation("フィールドスキーマのバックアップはスキップされました");
            return false;
        }

        this._logger?.LogInformation("フィールドスキーマを保存しています…");

        var json = JsonSerializer.Serialize(metadata, this._jsonOptions);

        var dir = this.Options.OutputPath.FullName;
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, this.Options.FieldSchemaFileName);

        if (File.Exists(filePath) && !this.Options.Overwrite) {
            this._logger?.LogWarning("fields.json が既に存在するためスキップされました: {Path}", filePath);
            return false;
        }

        try {
            // 5. 書き込み
            await File.WriteAllTextAsync(filePath, json);
            this._logger?.LogInformation("フィールドスキーマを保存しました: {Path}", filePath);
            return true;

        } catch (Exception ex) {
            this._logger?.LogError(ex, "フィールドスキーマの保存に失敗しました");
            return false;
        }
    }

}
