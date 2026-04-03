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
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Backup.Infrastructure.Services;

/// <summary>
/// バックアップサービスの実装
/// </summary>
/// <param name="schemaProvider">スキーマプロバイダー</param>
/// <param name="_accessFactory">Kintoneアクセスファクトリー</param>
/// <param name="httpClientFactory">HTTPクライアントファクトリー</param>
/// <param name="logger">ロガー</param>
public sealed class BackupService(
    ISchemaProvider schemaProvider,
    IKintoneAccessFactory _accessFactory,
    IHttpClientFactory httpClientFactory,
    IKintoneFieldParser fieldParser,
    ILogger<BackupService>? logger = null) : IBackupService {

    private IKintoneApi? _api;
    private readonly ISchemaProvider _schemaProvider = schemaProvider;
    private readonly IKintoneAccessFactory _accessFactory = _accessFactory;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IKintoneFieldParser _fieldParser = fieldParser;
    private JsonSerializerOptions? _jsonOptions;
    private readonly ILogger<BackupService>? _logger = logger;
    private int _partIndex = 0;
    /// <summary>
    /// part_xxxxx.json の xxxxx 部の桁数
    /// </summary>
    private const int PartDigits = 5;
    public BackupOptions Options { get; set; } = default!;
    public string BackupRoot { get; set; } = string.Empty;

    /// <summary>
    /// バックアップを実行します
    /// </summary>
    public async Task<BackupResult> RunBackupAsync() {
        this.EnsureApiInitialized();
        var result = new BackupResult();

        try {
            // 0) ディレクトリ作成
            this.BackupRoot = this.PrepareBackupDirectories();

            // 1) ページ単位で part_xxxxx.json を作成
            await foreach (var pageStream in this._api!.StreamCursorPagesAsync(this.Options.Query ?? "", this.Options.FieldCodes, this.Options.SplitSize)) {
                var partPath = this.CreateNextPartFilePath();
                await this.WritePrettyJsonAsync(pageStream, partPath);

                result.PartFiles.Add(Path.GetFileName(partPath));
                result.RecordCount += this.CountRecordsInPage(pageStream);
            }

            // 2) スキーマ取得・保存
            var access = this._accessFactory.CreateApiTokenAccess(this.Options.SubDomain, this.Options.ApiToken);
            var metadata = await this._schemaProvider.GetMetadataAsync(access.Domain, this.Options.ApiToken, this.Options.AppID);
            await this.SaveFieldSchemaAsync(metadata);
            await this.SaveLayoutSchemaAsync(metadata);
            result.SchemaSaved = true;

            // 3) 添付ファイルダウンロード（Stream）
            if (this.Options.DownloadFiles) {
                await foreach (var record in this._api!.StreamRecordsAsync(this.Options.Query ?? "", this.Options.FieldCodes, this.Options.SplitSize)) {
                    await this.DownloadFilesAsync(record);
                }
            }

            // 4) manifest.json 保存
            await this.SaveManifestAsync(metadata, result);

            result.BackedUpDirectory = new DirectoryInfo(this.BackupRoot);
            result.Success = true;

        } catch (Exception ex) {
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Kintone API 初期化を保証します
    /// </summary>
    private void EnsureApiInitialized() {
        if (this._api != null) { return; }
        ArgumentNullException.ThrowIfNull(this.Options);

        var access = new ApiTokenAccess(this.Options.SubDomain, this.Options.ApiToken);

        // this._httpClient ??= new HttpClient {
        //     BaseAddress = new Uri($"https://{access.Domain}/k/v1/")
        // };
        var httpClient = this._httpClientFactory.CreateClient("Kintone");

        this._jsonOptions ??= new JsonSerializerOptions() {
            WriteIndented = this.Options.Pretty,
            Encoder = this.Options.EscapeUnicode
                ? null
                : JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        this._api = new KintoneApi(access: access, appID: this.Options.AppID, httpClientFactory: this._httpClientFactory, jsonOptions: this._jsonOptions);

        // BatchSize が指定されていれば KintoneApi に反映
        if (this.Options.BatchSize is int size) {
            this._api.CursorPageSize = size; // KintoneApi 側でバリデーション
        }

        this._schemaProvider.SetDomain(this.Options.SubDomain);
    }

    /// <summary>
    /// 次の分割ファイルパスを作成します
    /// </summary>
    /// <returns>作成された分割ファイルのパス</returns>
    private string CreateNextPartFilePath() {
        this._partIndex++;

        // 99999 を超えたら 1 に戻す（実際に100kファイルは作らないので安全）
        if (this._partIndex > 99999) {
            this._partIndex = 1;
        }

        var fileName = $"part_{this._partIndex.ToString().PadLeft(PartDigits, '0')}.json";

        var path = Path.Combine(
            this.Options.OutputPath.FullName,   // 例: /backup/20250130_120000/
            "data",
            fileName
        );

        return path;
    }

    /// <summary>
    /// ストリーム内のレコード数をカウントします
    /// </summary>
    /// <param name="pageStream">レコードが含まれるストリーム</param>
    /// <returns>レコード数</returns>
    private int CountRecordsInPage(Stream pageStream) {
        if (pageStream.CanSeek) { pageStream.Position = 0; }

        using var doc = JsonDocument.Parse(pageStream);

        if (!doc.RootElement.TryGetProperty("records", out var records)) { return 0; }

        return records.ValueKind switch {
            JsonValueKind.Array => records.GetArrayLength(),

            // オブジェクト形式の場合はプロパティ数がレコード数
            JsonValueKind.Object => records.EnumerateObject().Count(),
            _ => 0
        };
    }

    /// <summary>
    /// バックアップ先ディレクトリを準備します
    /// </summary>
    /// <returns>作成されたバックアップ先ディレクトリのパス</returns>
    /// <exception cref="IOException">バックアップ先ディレクトリの作成に失敗した場合</exception>
    private string PrepareBackupDirectories() {
        this._logger?.LogInformation("バックアップ先ディレクトリを準備します: App={App}", this.Options.AppID);

        // AppID フォルダ（ゼロパディング）
        var appIdFolder = $"AppID{this.Options.AppID:D6}";

        // タイムスタンプフォルダ（UTC）
        var timestampFolder = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        // ルートパス
        var backupRoot = Path.Combine(this.Options.OutputPath.FullName, appIdFolder, timestampFolder);

        // Overwrite == false の場合は既存チェック
        if (!this.Options.Overwrite && Directory.Exists(backupRoot)) {
            var message = $"バックアップ先ディレクトリが既に存在するため、バックアップを中止しました: {backupRoot}";
            this._logger?.LogError("{message}", message);
            throw new IOException(message);
        }

        // ディレクトリ作成
        Directory.CreateDirectory(backupRoot);
        Directory.CreateDirectory(Path.Combine(backupRoot, "data"));
        Directory.CreateDirectory(Path.Combine(backupRoot, "files"));

        // BackupService 内で OutputPath を書き換え
        this.Options.OutputPath = new DirectoryInfo(backupRoot);

        this._logger?.LogInformation("バックアップ先ディレクトリを作成しました: {Path}", backupRoot);

        return backupRoot;
    }

    /// <summary>
    /// レコードをストリームで取得します
    /// </summary>
    /// <param name="output">出力先のストリーム</param>
    /// <returns>非同期操作のタスク</returns>
    private async Task FetchRecordsAsStreamAsync(Stream output) {
        if (!string.IsNullOrWhiteSpace(this.Options.Query)) {
            await this._api!.RawFindByQueryAsStreamAsync(
                output,
                this.Options.Query!,
                fieldCodes: this.Options.FieldCodes
            );
            return;
        }

        await this._api!.RawFindAllAsStreamAsync(output, fieldCodes: this.Options.FieldCodes);
    }

    /// <summary>
    /// ストリーム内のレコード数をカウントします
    /// </summary>
    /// <param name="input">レコードが含まれるストリーム</param>
    /// <returns>レコード数</returns>
    private async Task<int> CountRecordsInStreamAsync(Stream input) {
        // Utf8JsonReader は同期 API なので、Stream を一括読み込みする必要がある
        // ただし byte[] は UTF-8 のままなので string よりはるかに軽い
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        var span = new ReadOnlySpan<byte>(ms.GetBuffer(), 0, (int)ms.Length);

        var reader = new Utf8JsonReader(span, new JsonReaderOptions {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        int count = 0;
        bool insideRecordsArray = false;

        while (reader.Read()) {
            // "records": [ ... ] の開始を検出
            if (reader.TokenType == JsonTokenType.PropertyName &&
                reader.ValueTextEquals("records")) {
                // 次のトークンが StartArray のはず
                reader.Read();
                if (reader.TokenType == JsonTokenType.StartArray) {
                    insideRecordsArray = true;
                }
                continue;
            }

            // records 配列の中にいる間だけカウント
            if (insideRecordsArray) {
                if (reader.TokenType == JsonTokenType.StartObject) {
                    count++;
                } else if (reader.TokenType == JsonTokenType.EndArray) {
                    // 配列の終わり
                    break;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// ストリームから分割された JSON ファイルを保存します
    /// </summary>
    /// <param name="input">レコードが含まれるストリーム</param>
    /// <returns>作成された分割ファイルのパスのリスト</returns>
    private async Task<List<string>> SaveSplitJsonFilesFromStreamAsync(Stream input) {
        var result = new List<string>();
        var buffer = new byte[8192];

        // Utf8JsonReader は同期 API のため、MemoryStream に読み込む
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        var span = new ReadOnlySpan<byte>(ms.GetBuffer(), 0, (int)ms.Length);

        var reader = new Utf8JsonReader(span, new JsonReaderOptions {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        // 出力ファイル管理
        int partIndex = 1;
        int currentSize = 0;
        FileStream? currentFile = null;
        Utf8JsonWriter? writer = null;

        void OpenNewFile() {
            currentFile?.Dispose();
            writer?.Dispose();

            var fileName = $"part_{partIndex:D5}.json";
            var filePath = Path.Combine(this.Options.OutputPath.FullName, "data", fileName);

            currentFile = File.Create(filePath);
            writer = new Utf8JsonWriter(currentFile, new JsonWriterOptions {
                Indented = this.Options.Pretty,
                SkipValidation = false
            });

            writer.WriteStartObject();
            writer.WritePropertyName("records");
            writer.WriteStartArray();

            result.Add(fileName);
            currentSize = 0;
            partIndex++;
        }

        void CloseCurrentFile() {
            if (writer != null) {
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
            }

            writer?.Dispose();
            currentFile?.Dispose();
        }

        bool insideRecordsArray = false;

        while (reader.Read()) {
            // "records": [ の開始を検出
            if (reader.TokenType == JsonTokenType.PropertyName &&
                reader.ValueTextEquals("records")) {
                reader.Read(); // StartArray
                insideRecordsArray = true;
                continue;
            }

            if (!insideRecordsArray) {
                continue;
            }

            // records 配列の終わり
            if (reader.TokenType == JsonTokenType.EndArray) {
                break;
            }

            // レコードオブジェクトの開始
            if (reader.TokenType == JsonTokenType.StartObject) {
                // レコード全体を抽出する
                var recordJson = ExtractJsonObject(ref reader, span);

                // 新しいファイルが必要か？
                if (writer == null || currentSize + recordJson.Length > this.Options.SplitSize) {
                    CloseCurrentFile();
                    OpenNewFile();
                }

                // 書き込み
                writer!.WriteRawValue(recordJson);
                currentSize += recordJson.Length;
            }
        }

        CloseCurrentFile();
        return result;
    }

    /// <summary>
    /// レコード内の添付ファイルをダウンロードします
    /// </summary>
    /// <param name="record">添付ファイルを含むレコードの JSON 要素</param>
    /// <returns>非同期操作のタスク</returns>
    private async Task DownloadFilesAsync(JsonElement record) {
        // レコード番号を取得
        if (!record.TryGetProperty("$id", out var idProp) || !idProp.TryGetProperty("value", out var idValueProp)) {
            this._logger?.LogWarning("レコード番号が見つかりません。添付ファイルのダウンロードをスキップします。");
            return;
        }
        var recordId = idValueProp.GetString() ?? "unknown";

        foreach (var property in record.EnumerateObject()) {
            var field = property.Value;
            var fieldCode = property.Name;

            // フィールドがオブジェクトでなければスキップ
            if (field.ValueKind != JsonValueKind.Object) { continue; }

            // type が FILE でなければスキップ
            if (!field.TryGetProperty("type", out var typeProp)) { continue; }
            if (!string.Equals(typeProp.GetString(), "FILE", StringComparison.OrdinalIgnoreCase)) { continue; }

            // value が配列でなければスキップ
            if (!field.TryGetProperty("value", out var valueProp)) { continue; }
            if (valueProp.ValueKind != JsonValueKind.Array) { continue; }

            // 保存先ディレクトリ
            var fieldDir = Path.Combine(this.BackupRoot, "files", recordId, fieldCode);
            Directory.CreateDirectory(fieldDir);

            // 添付ファイルを列挙
            foreach (var item in valueProp.EnumerateArray()) {
                if (!item.TryGetProperty("fileKey", out var fileKeyProp)) { continue; }

                var fileKey = fileKeyProp.GetString();
                if (string.IsNullOrEmpty(fileKey)) { continue; }

                var fileName = item.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString() ?? $"{fileKey}.bin"
                    : $"{fileKey}.bin";

                var savePath = Path.Combine(fieldDir, fileName);

                if (File.Exists(savePath)) { continue; }

                try {
                    var bytes = await this._api!.DownloadFileAsync(fileKey);
                    await File.WriteAllBytesAsync(savePath, bytes);

                    this._logger?.LogInformation("Downloaded file: {FileName}", fileName);

                } catch (Exception ex) {
                    this._logger?.LogError(ex, "Failed to download file: {FileKey}", fileKey);
                }
            }
        }
    }

    /// <summary>
    /// ストリームから添付ファイルをダウンロードします
    /// </summary>
    /// <param name="input">レコードが含まれるストリーム</param>
    /// <returns>非同期操作のタスク</returns>
    private async Task<(int success, int fail)> DownloadFilesWithResultFromStreamAsync(Stream input) {
        // Stream → MemoryStream（UTF-8 のままなので軽量）
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        var span = new ReadOnlySpan<byte>(ms.GetBuffer(), 0, (int)ms.Length);

        var files = this.ExtractFileInfos(span);

        int success = 0;
        int fail = 0;

        foreach (var (fileKey, fileName) in files) {
            try {
                await this.DownloadSingleFileAsync(fileKey, fileName);
                success++;
            } catch {
                fail++;
            }
        }

        return (success, fail);
    }

    /// <summary>
    /// ストリームから添付ファイルの情報を抽出します
    /// </summary>
    /// <param name="span">JSON データを含むバイト配列のスパン</param>
    /// <returns>抽出されたファイル情報のリスト</returns>
    private List<(string fileKey, string fileName)> ExtractFileInfos(ReadOnlySpan<byte> span) {
        var list = new List<(string fileKey, string fileName)>();

        var reader = new Utf8JsonReader(span, new JsonReaderOptions {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        bool insideRecordsArray = false;

        while (reader.Read()) {
            // "records": [
            if (reader.TokenType == JsonTokenType.PropertyName &&
                reader.ValueTextEquals("records")) {
                reader.Read(); // StartArray
                insideRecordsArray = true;
                continue;
            }

            if (!insideRecordsArray) { continue; }

            // records 配列の終わり
            if (reader.TokenType == JsonTokenType.EndArray) { break; }

            // レコードオブジェクトの開始
            if (reader.TokenType == JsonTokenType.StartObject) {
                var recordJson = ExtractJsonObject(ref reader, span);

                using var doc = JsonDocument.Parse(recordJson);
                var root = doc.RootElement;

                foreach (var prop in root.EnumerateObject()) {
                    if (prop.Value.ValueKind != JsonValueKind.Array) { continue; }
                    if (!IsFileField(prop.Value)) { continue; }

                    foreach (var fileInfo in prop.Value.EnumerateArray()) {
                        var fileKey = fileInfo.GetProperty("fileKey").GetString();
                        var fileName = fileInfo.GetProperty("name").GetString();

                        if (fileKey != null && fileName != null) {
                            list.Add((fileKey, fileName));
                        }
                    }
                }
            }
        }

        return list;
    }

    /// <summary>
    /// 単一ファイルをダウンロードします
    /// </summary>
    /// <param name="fileKey">ダウンロードするファイルのキー</param>
    /// <param name="fileName">ダウンロードするファイルの名前</param>
    /// <returns>非同期操作のタスク</returns>
    private async Task DownloadSingleFileAsync(string fileKey, string fileName) {
        // 保存先パス: {OutputPath}/files/{fileName}
        var destPath = Path.Combine(this.Options.OutputPath.FullName, "files", fileName);
        var destFile = new FileInfo(destPath);

        // KintoneApi.File.DownloadFileAsync を呼ぶだけ
        await this._api!.DownloadFileAsync(fileKey, destFile);
    }

    /// <summary>
    /// JSON オブジェクトを抽出します
    /// </summary>
    /// <param name="reader">JSON リーダー</param>
    /// <param name="source">JSON データを含むバイト配列のスパン</param>
    /// <returns></returns>
    private static string ExtractJsonObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> source) {
        var start = reader.TokenStartIndex;
        int depth = 0;

        do {
            if (reader.TokenType == JsonTokenType.StartObject) {
                depth++;
            } else if (reader.TokenType == JsonTokenType.EndObject) {
                depth--;
            }

            reader.Read();
        }
        while (depth > 0);

        var end = reader.TokenStartIndex;
        var length = (int)(end - start);

        return Encoding.UTF8.GetString(source.Slice((int)start, length));
    }

    /// <summary>
    /// ファイルフィールドかどうかを判定します
    /// </summary>
    /// <param name="element">判定する JSON 要素</param>
    /// <returns>ファイルフィールドであれば true、それ以外は false</returns>
    private static bool IsFileField(JsonElement element) {
        if (element.ValueKind != JsonValueKind.Array) {
            return false;
        }

        if (!element.EnumerateArray().Any()) {
            return false;
        }

        var first = element.EnumerateArray().First();

        return first.TryGetProperty("fileKey", out _)
            && first.TryGetProperty("name", out _);
    }

    /// <summary>
    /// JSON を保存します
    /// </summary>
    /// <param name="json">保存する JSON データ</param>
    /// <param name="fileName">保存するファイル名</param>
    /// <param name="directory">保存先ディレクトリ（省略可能）</param>
    /// <returns>非同期操作のタスク</returns>
    /// <exception cref="IOException">ファイルの保存に失敗した場合にスローされます</exception>
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
    /// マニフェストを保存します
    /// </summary>
    /// <param name="metadata">アプリのメタデータ</param>
    /// <param name="result">バックアップ結果</param>
    /// <returns>非同期操作のタスク</returns>
    private async Task SaveManifestAsync(KintoneAppMetadata metadata, BackupResult result) {
        var manifest = new BackupManifest {
            AppId = this.Options.AppID,
            AppRevision = metadata.Revision,
            BackupAt = DateTime.UtcNow,
            RecordCount = result.RecordCount,
            FileFieldCount = metadata.Fields.Count(f => f.FieldType == KintoneFieldType.File),
            FileCount = result.FileDownloadedCount,
            PartFiles = result.PartFiles,
            SplitSize = this.Options.SplitSize,
            Options = new {
                this.Options.IncludeFieldSchema,
                this.Options.DownloadFiles,
                this.Options.Overwrite,
                this.Options.Query,
                this.Options.FieldCodes,
            },
        };
        var path = Path.Combine(this.Options.OutputPath.FullName, "manifest.json");
        if (File.Exists(path) && !this.Options.Overwrite) {
            this._logger?.LogWarning("マニフェストの保存がスキップされました: {Path}", path);
            return;
        }
        this._logger?.LogInformation("マニフェストを保存しています: {Path}", path);
        var json = JsonSerializer.Serialize(manifest, this._jsonOptions);
        await this.SaveJsonAsync(json, "manifest.json");
    }

    /// <summary>
    /// フィールドスキーマを保存します
    /// </summary>
    /// <param name="metadata">アプリのメタデータ</param>
    /// <returns>非同期操作のタスク</returns>
    private async Task<bool> SaveFieldSchemaAsync(KintoneAppMetadata metadata) {
        if (!this.Options.IncludeFieldSchema) {
            this._logger?.LogInformation("フィールドスキーマのバックアップはスキップされました");
            return false;
        }

        this._logger?.LogInformation("フィールドスキーマを保存しています…");

        var access = this._accessFactory.CreateApiTokenAccess(this.Options.SubDomain, this.Options.ApiToken);
        var metadataApi = new KintoneAppMetadataApi(this._httpClientFactory, this._fieldParser, this._logger as ILogger<KintoneAppMetadataApi>);
        var json = await metadataApi.GetFieldsJsonAsync(access.Domain, this.Options.ApiToken, metadata.AppId);

        var dir = this.Options.OutputPath.FullName;
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, this.Options.FieldSchemaFileName);

        if (File.Exists(filePath) && !this.Options.Overwrite) {
            this._logger?.LogWarning("fields.json が既に存在するためスキップされました: {Path}", filePath);
            return false;
        }

        try {
            // JSON を整形して書き込む
            using var doc = JsonDocument.Parse(json);

            using var fs = File.Create(filePath);
            using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions {
                Indented = this._jsonOptions!.WriteIndented,
                Encoder = this._jsonOptions!.Encoder
            });

            doc.WriteTo(writer);

            this._logger?.LogInformation("フィールドスキーマを保存しました: {Path}", filePath);
            return true;

        } catch (Exception ex) {
            this._logger?.LogError(ex, "フィールドスキーマの保存に失敗しました");
            return false;
        }
    }

    private async Task<bool> SaveLayoutSchemaAsync(KintoneAppMetadata metadata) {
        if (!this.Options.IncludeFieldSchema) {
            this._logger?.LogInformation("レイアウトスキーマのバックアップはスキップされました");
            return false;
        }

        this._logger?.LogInformation("レイアウトスキーマを保存しています…");

        var access = this._accessFactory.CreateApiTokenAccess(this.Options.SubDomain, this.Options.ApiToken);
        var metadataApi = new KintoneAppMetadataApi(this._httpClientFactory, this._fieldParser, this._logger as ILogger<KintoneAppMetadataApi>);
        var json = await metadataApi.GetLayoutJsonAsync(access.Domain, this.Options.ApiToken, metadata.AppId);

        var dir = this.Options.OutputPath.FullName;
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, "layout.json");

        if (File.Exists(filePath) && !this.Options.Overwrite) {
            this._logger?.LogWarning("layout.json が既に存在するためスキップされました: {Path}", filePath);
            return false;
        }

        try {
            using var doc = JsonDocument.Parse(json);

            using var fs = File.Create(filePath);
            using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions {
                Indented = this._jsonOptions!.WriteIndented,
                Encoder = this._jsonOptions!.Encoder
            });

            doc.WriteTo(writer);

            this._logger?.LogInformation("レイアウトスキーマを保存しました: {Path}", filePath);
            return true;
        } catch (Exception ex) {
            this._logger?.LogError(ex, "レイアウトスキーマの保存に失敗しました");
            return false;
        }
    }

    /// <summary>
    /// ストリームから整形された JSON ファイルを書き込みます
    /// </summary>
    /// <param name="input">入力ストリーム</param>
    /// <param name="path">出力ファイルのパス</param>
    /// <returns>非同期操作のタスク</returns>
    private async Task WritePrettyJsonAsync(Stream input, string path) {
        if (input.CanSeek) { input.Position = 0; }

        using var doc = await JsonDocument.ParseAsync(input);

        using var fs = File.Create(path);
        using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions {
            Indented = this._jsonOptions!.WriteIndented,
            Encoder = this._jsonOptions!.Encoder
        });

        doc.WriteTo(writer);
    }

}
