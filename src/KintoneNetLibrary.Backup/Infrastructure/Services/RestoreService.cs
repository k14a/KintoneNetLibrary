using System.Text.Json;
using System.Text.Json.Nodes;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Backup.Application.DTOs;
using KintoneNetLibrary.Backup.Application.Interfaces;
using KintoneNetLibrary.Backup.Domain.Enums;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Converters;
using KintoneNetLibrary.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;

namespace KintoneNetLibrary.Backup.Infrastructure.Services;

/// <summary>
/// バックアップデータから Kintone アプリにレコードをリストアするサービスクラスです。
/// </summary>
/// <param name="schemaProvider">スキーマプロバイダー</param>
/// <param name="accessFactory">Kintoneアクセスファクトリー</param>
/// <param name="metadataApi">アプリメタデータ API</param>
/// <param name="apiFactory">Kintone API ファクトリー</param>
/// <param name="logger">ロガー</param>
public sealed class RestoreService(
    ISchemaProvider schemaProvider,
    IKintoneAccessFactory accessFactory,
    IKintoneAppMetadataApi metadataApi,
    IKintoneApiFactory apiFactory,
    ILogger<RestoreService>? logger = null) : IRestoreService {

    private IKintoneApi? _api;
    private readonly IKintoneAccessFactory _accessFactory = accessFactory;
    private readonly ISchemaProvider _schemaProvider = schemaProvider;
    private readonly IKintoneAppMetadataApi _metadataApi = metadataApi;
    private readonly IKintoneApiFactory _apiFactory = apiFactory;
    private readonly ILogger? _logger = logger;
    private static readonly HashSet<string> _readonlyFieldTypes = [
        "CREATOR",
        "MODIFIER",
        "CREATED_TIME",
        "UPDATED_TIME",
        "RECORD_NUMBER",
        "REFERENCE_TABLE",
        "LOOKUP",
    ];

    private RestoreOptions? _options;
    private RestoreOptions Options => this._options
        ?? throw new InvalidOperationException($"{nameof(RunRestoreAsync)} を先に呼び出してください。");

    /// <summary>
    /// リストアを実行します
    /// </summary>
    /// <param name="options">リストアオプション</param>
    /// <returns>リストア結果</returns>
    /// <exception cref="NotSupportedException">サポートされていない操作が指定された場合にスローされます。</exception>
    public async Task<RestoreResult> RunRestoreAsync(RestoreOptions options) {
        this._options = options;
        this._logger?.LogInformation("リストア 開始: App={App}", this.Options.AppId);
        this.EnsureApiInitialized();

        var result = new RestoreResult();
        try {
            // 0) DryRun モード確認
            if (this.Options.DryRun) {
                this._logger?.LogWarning("DryRun モードで実行します。Kintone への書き込みは行われません。");
            }

            // 1) バックアップディレクトリ検証
            var manifest = await this.LoadManifestAsync();
            var schema = await this.LoadFieldSchemaAsync();
            this.ValidateBackupDirectory(manifest);

            // 2) スキーマ検証（Force=false の場合）
            if (!this.Options.Force) {
                await this.ValidateSchemaAsync(schema);
            }

            // 3) RestoreMode に応じた前処理
            if (this.Options.Mode == RestoreMode.FullReplace) {
                await this.DeleteAllRecordsAsync(result);
            }

            // 4) data/part-xxxx.json を順次読み込み、レコード復元
            var partFiles = this.GetPartFiles(manifest.PartFiles);
            int partIndex = 1;

            foreach (var partFile in partFiles) {
                this._logger?.LogInformation("パート {Index}/{Total} を処理中: {File}",
                    partIndex, manifest.Parts, partFile);

                var records = await this.LoadPartRecordsAsync(partFile);

                // 添付ファイルの処理
                records = await this.ReplaceFileKeysAsync(records);

                await this.RestoreRecordsAsync(records, result);

                partIndex++;
            }

            // 5) 結果を返す
            result.Success = true;
            return result;

        } catch (Exception ex) {
            this._logger?.LogError(ex, "リストア中にエラーが発生しました");
            result.Success = false;
            result.Errors.Add(ex.Message);
            return result;

        } finally {
            this._logger?.LogInformation("リストア 完了");
        }
    }

    /// <summary>
    /// Kintone API の初期化を行います
    /// </summary>
    private void EnsureApiInitialized() {
        if (this._api != null) { return; }
        var access = new ApiTokenAccess(this.Options.SubDomain, this.Options.ApiToken);

        this._api = this._apiFactory.Create(access, this.Options.AppId);
    }

    /// <summary>
    /// manifest.json を読み込みます
    /// </summary>
    /// <returns>バックアップマニフェスト</returns>
    /// <exception cref="FileNotFoundException">manifest.json が見つからない場合にスローされます</exception>
    /// <exception cref="InvalidOperationException">manifest.json の読み込みに失敗した場合にスローされます</exception>
    private async Task<BackupManifest> LoadManifestAsync() {
        var path = Path.Combine(this.Options.BackupRootPath.FullName, "manifest.json");

        if (!File.Exists(path)) {
            throw new FileNotFoundException($"manifest.json が見つかりません: {path}");
        }

        using var stream = File.OpenRead(path);
        var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(
            stream,
            new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            }
        ) ?? throw new InvalidOperationException("manifest.json の読み込みに失敗しました。");

        this._logger?.LogInformation(
            "manifest.json 読み込み完了: Records={RecordCount}, Parts={Parts}, SplitSize={SplitSize}",
            manifest.RecordCount,
            manifest.Parts,
            manifest.SplitSize
        );

        return manifest;
    }

    /// <summary>
    /// fields.json を読み込みます
    /// </summary>
    /// <returns>フィールドスキーマの JSON ノード</returns>
    /// <exception cref="FileNotFoundException">fields.json が見つからない場合にスローされます</exception>
    /// <exception cref="InvalidOperationException">fields.json の読み込みに失敗した場合にスローされます</exception>
    private async Task<JsonNode> LoadFieldSchemaAsync() {
        var path = Path.Combine(this.Options.BackupRootPath.FullName, "fields.json");
        if (!File.Exists(path)) {
            throw new FileNotFoundException("fields.json が見つかりません", path);
        }
        var json = await File.ReadAllTextAsync(path);
        return JsonNode.Parse(json)!;
    }

    /// <summary>
    /// バックアップデータを読み込みます
    /// </summary>
    /// <param name="partFiles">分割ファイルのリスト</param>
    /// <returns>分割ファイルのパスの列挙</returns>
    /// <exception cref="FileNotFoundException">バックアップデータファイルが見つからない場合にスローされます</exception>
    private IEnumerable<string> GetPartFiles(IList<string> partFiles) {
        var dataDir = Path.Combine(this.Options.BackupRootPath.FullName, "data");

        foreach (var fileName in partFiles) {
            var fullPath = Path.Combine(dataDir, fileName);

            if (!File.Exists(fullPath)) {
                throw new FileNotFoundException($"バックアップデータファイルが見つかりません: {fullPath}");
            }

            yield return fullPath;
        }
    }

    /// <summary>
    /// 分割ファイルからレコードデータを読み込みます
    /// </summary>
    /// <param name="partFilePath">分割ファイルのパス</param>
    /// <returns>レコードデータのリスト</returns>
    /// <exception cref="FileNotFoundException">分割ファイルが見つからない場合にスローされます</exception>
    /// <exception cref="InvalidOperationException">分割ファイルの読み込みに失敗した場合にスローされます</exception>
    private async Task<List<JsonNode>> LoadPartRecordsAsync(string partFilePath) {
        if (!File.Exists(partFilePath)) {
            throw new FileNotFoundException($"分割ファイルが見つかりません: {partFilePath}");
        }

        using var stream = File.OpenRead(partFilePath);
        var doc = await JsonNode.ParseAsync(stream) ?? throw new InvalidOperationException($"分割ファイルの JSON パースに失敗しました: {partFilePath}");
        var recordsNode = (doc["records"]?.AsArray()) ?? throw new InvalidOperationException($"records 配列が存在しません: {partFilePath}");
        var list = new List<JsonNode>(recordsNode.Count);

        foreach (var record in recordsNode) {
            if (record is null) {
                continue;
            }

            // JsonNode をそのまま保持（fileKey 差し替えで編集可能）
            list.Add(record.DeepClone());
        }

        return list;
    }

    /// <summary>
    /// 添付ファイルの fileKey を置換します
    /// </summary>
    /// <param name="records">レコードデータのリスト</param>
    /// <returns>更新されたレコードデータのリスト</returns>
    /// <exception cref="InvalidOperationException">fileKey の置換に失敗した場合にスローされます</exception>
    private async Task<List<JsonNode>> ReplaceFileKeysAsync(List<JsonNode> records) {
        if (this.Options.DryRun) {
            foreach (var record in records) {
                var recordId = record["$id"]?["value"]?.ToString();
                this._logger?.LogInformation("DryRun: レコード {RecordId} の添付ファイル fileKey を差し替える予定です", recordId);
            }

            return records;
        }

        var updatedRecords = new List<JsonNode>(records.Count);

        foreach (var recordNode in records) {
            var node = recordNode.AsObject();

            var recordId = node["$id"]?["value"]?.ToString()
                ?? throw new InvalidOperationException("レコードに $id が存在しません。");

            foreach (var field in node) {
                var fieldCode = field.Key;
                var fieldObj = field.Value?.AsObject();
                if (fieldObj is null) { continue; }

                if (fieldObj["type"]?.ToString() != "FILE") { continue; }

                var fileArray = fieldObj["value"]?.AsArray();
                if (fileArray is null) { continue; }

                if (!this.Options.RestoreFiles) {
                    // Options.RestoreFiles が false の場合、空配列にする
                    fieldObj["value"] = new JsonArray();
                    continue;
                }

                for (int i = 0; i < fileArray.Count; i++) {
                    var fileNode = fileArray[i]!.AsObject();
                    var fileName = fileNode["name"]!.ToString();

                    var localPath = Path.Combine(
                        this.Options.BackupRootPath.FullName,
                        "files",
                        recordId,
                        fieldCode,
                        fileName
                    );

                    var fi = new FileInfo(localPath);

                    using var fs = fi.OpenRead();
                    var newFileKey = await this._api!.UploadFileAsync(fs, fileName);

                    fileNode["fileKey"] = newFileKey;
                }
            }
            updatedRecords.Add(node);
        }
        return updatedRecords;
    }

    /// <summary>
    /// レコード復元を実行します
    /// </summary>
    /// <param name="records">レコードデータのリスト</param>
    /// <param name="result">リストア結果</param>
    private async Task RestoreRecordsAsync(List<JsonNode> records, RestoreResult result) {
        switch (this.Options.Mode) {
            case RestoreMode.FullReplace:
                await this.RestoreRecordsCreateOnlyAsync(records, result);
                break;
            case RestoreMode.Merge:
                await this.RestoreRecordsCreateOnlyAsync(records, result, true);
                break;
            case RestoreMode.Upsert:
                await this.RestoreRecordsUpsertAsync(records, result);
                break;
            default:
                throw new NotSupportedException($"Unsupported RestoreMode: {this.Options.Mode}");
        }
    }

    /// <summary>
    /// レコードを一括インサートします
    /// </summary>
    /// <param name="records">レコードデータのリスト</param>
    /// <param name="result">リストア結果</param>
    /// <param name="skipExisting">既存レコードをスキップするかどうか</param>
    private async Task RestoreRecordsCreateOnlyAsync(List<JsonNode> records, RestoreResult result, bool skipExisting = false) {
        if (this.Options.DryRun) {
            int count = 0;

            foreach (var record in records) {
                var id = record["$id"]?["value"]?.ToString();

                if (skipExisting && id is not null) {
                    this._logger?.LogInformation("DryRun: 既存レコード {Id} はスキップされます", id);
                    continue;
                }

                this._logger?.LogInformation("DryRun: 新規作成予定のレコード: $id={Id}", id);
                count++;
            }

            this._logger?.LogInformation("DryRun: 新規作成予定件数 = {Count}", count);
            result.AddedRecords += count;
            return;
        }

        var batch = new List<JsonNode>(this.Options.BatchSize);

        foreach (var record in records) {
            var id = record["$id"]?["value"]?.ToString();

            if (skipExisting && id is not null) {
                // 既存レコードはスキップ（Merge モード）
                continue;
            }

            // $id と $revision を削除して新規作成にする
            var newRecord = record.AsObject();
            newRecord.Remove("$id");
            newRecord.Remove("$revision");
            this.RemoveRecordIdFields(newRecord);
            this.RemoveReadonlyFields(newRecord);

            batch.Add(newRecord);

            // バッチサイズに達したら一括登録
            if (batch.Count >= this.Options.BatchSize) {
                await this.InsertBatchAsync(batch, result);
                batch.Clear();
            }
        }
        if (batch.Count > 0) {
            await this.InsertBatchAsync(batch, result);
        }
    }

    /// <summary>
    /// レコードを一括アップサートします
    /// </summary>
    /// <param name="records">レコードデータのリスト</param>
    /// <param name="result">リストア結果</param>
    private async Task RestoreRecordsUpsertAsync(List<JsonNode> records, RestoreResult result) {
        if (this.Options.DryRun) {
            int postCount = 0;
            int putCount = 0;

            foreach (var record in records) {
                var id = record["$id"]?["value"]?.ToString();

                if (id is null) {
                    this._logger?.LogInformation("DryRun: 新規作成予定のレコード");
                    postCount++;
                } else {
                    this._logger?.LogInformation("DryRun: 更新予定のレコード: $id={Id}", id);
                    putCount++;
                }
            }

            this._logger?.LogInformation("DryRun: POST（新規）予定 = {Post}, PUT（更新）予定 = {Put}", postCount, putCount);

            result.AddedRecords += postCount;
            result.UpdatedRecords += putCount;
            return;
        }

        var postBatch = new List<JsonNode>(this.Options.BatchSize);
        var putBatch = new List<JsonNode>(this.Options.BatchSize);

        foreach (var record in records) {
            var id = record["$id"]?["value"]?.ToString();

            if (id is null) {
                // POST(新規作成)
                var newRecord = record.DeepClone().AsObject();
                newRecord.Remove("$id");
                newRecord.Remove("$revision");
                this.RemoveRecordIdFields(newRecord);
                this.RemoveReadonlyFields(newRecord);

                postBatch.Add(newRecord);
                if (postBatch.Count >= this.Options.BatchSize) {
                    await this.InsertBatchAsync(postBatch, result);
                    postBatch.Clear();
                }
            } else {
                // PUT(更新)
                var updateRecord = record.DeepClone().AsObject();
                updateRecord.Remove("$id");
                updateRecord.Remove("$revision");

                this.RemoveRecordIdFields(updateRecord);
                this.RemoveReadonlyFields(updateRecord);

                var wrapper = new JsonObject {
                    ["id"] = id,
                    ["record"] = updateRecord
                };
                putBatch.Add(wrapper);

                if (putBatch.Count >= this.Options.BatchSize) {
                    await this.UpdateBatchAsync(putBatch, result);
                    putBatch.Clear();
                }
            }
        }

        // 端数処理
        if (postBatch.Count > 0) {
            await this.InsertBatchAsync(postBatch, result);
        }
        if (putBatch.Count > 0) {
            await this.UpdateBatchAsync(putBatch, result);
        }
    }

    /// <summary>
    /// レコード内の $id フィールドを削除します
    /// </summary>
    /// <param name="obj">レコードデータのオブジェクト</param>
    private void RemoveRecordIdFields(JsonObject obj) {
        // $id を削除
        obj.Remove("$id");

        // サブテーブルの行 Id を削除
        foreach (var kv in obj.ToList()) {
            if (kv.Value is JsonObject childObj) {
                // SUBTABLE の場合
                if (childObj["type"]?.ToString() == "SUBTABLE") {
                    var rows = childObj["value"]?.AsArray();
                    if (rows != null) {
                        foreach (var row in rows) {
                            var rowObj = row!.AsObject();
                            rowObj.Remove("id"); // ★ 行 Id 削除
                            this.RemoveRecordIdFields(rowObj["value"]!.AsObject());
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 読み取り専用フィールドを削除します
    /// </summary>
    /// <param name="record">レコードデータのオブジェクト</param>
    private void RemoveReadonlyFields(JsonObject record) {
        var toRemove = new List<string>();

        foreach (var kv in record.ToList()) {
            if (kv.Value is not JsonObject fieldObj) { continue; }

            var type = fieldObj["type"]?.ToString();
            if (type != null && _readonlyFieldTypes.Contains(type)) {
                toRemove.Add(kv.Key);
            }
        }

        foreach (var key in toRemove) {
            record.Remove(key);
        }
    }

    /// <summary>
    /// レコードを一括登録します
    /// </summary>
    /// <param name="batch">レコードデータのリスト</param>
    /// <param name="result">リストア結果</param>
    private async Task InsertBatchAsync(List<JsonNode> batch, RestoreResult result) {
        if (batch.Count == 0) { return; }

        var root = new JsonObject {
            ["app"] = this.Options.AppId,
            ["records"] = new JsonArray(batch.ToArray())
        };

        var json = root.ToJsonString();
        await this._api!.RawCreateAsync(json);
        result.AddedRecords += batch.Count;
    }

    /// <summary>
    /// レコードを一括更新します
    /// </summary>
    /// <param name="batch">レコードデータのリスト</param>
    /// <param name="result">リストア結果</param>
    private async Task UpdateBatchAsync(List<JsonNode> batch, RestoreResult result) {
        if (batch.Count == 0) { return; }

        var root = new JsonObject {
            ["app"] = this.Options.AppId,
            ["records"] = new JsonArray(batch.ToArray())
        };

        await this._api!.RawUpdateAsync(root.ToJsonString());

        result.UpdatedRecords += batch.Count;
    }

    /// <summary>
    /// バックアップディレクトリの妥当性を検証します
    /// </summary>
    /// <param name="manifest">バックアップマニフェスト</param>
    /// <exception cref="DirectoryNotFoundException">バックアップディレクトリが存在しない場合にスローされます</exception>
    /// <exception cref="FileNotFoundException">必要なファイルが存在しない場合にスローされます</exception>
    private void ValidateBackupDirectory(BackupManifest manifest) {
        var root = this.Options.BackupRootPath;

        if (!root.Exists) {
            throw new DirectoryNotFoundException($"バックアップディレクトリが存在しません: {root.FullName}");
        }

        // 1) manifest.json
        var manifestPath = Path.Combine(root.FullName, "manifest.json");
        if (!File.Exists(manifestPath)) {
            throw new FileNotFoundException($"manifest.json が見つかりません: {manifestPath}");
        }

        // 2) fields.json
        var fieldsPath = Path.Combine(root.FullName, "fields.json");
        if (!File.Exists(fieldsPath)) {
            throw new FileNotFoundException($"fields.json が見つかりません: {fieldsPath}");
        }

        // 3) data ディレクトリ
        var dataDir = Path.Combine(root.FullName, "data");
        if (!Directory.Exists(dataDir)) {
            throw new DirectoryNotFoundException($"data ディレクトリが見つかりません: {dataDir}");
        }

        // 4) parts と data/part-xxxx.json の整合性チェック
        foreach (var relativePath in manifest.PartFiles) {
            var fullPath = Path.Combine(root.FullName, "data", relativePath);
            if (!File.Exists(fullPath)) {
                throw new FileNotFoundException($"分割ファイルが不足しています: {fullPath}");
            }
        }

        // 5) files ディレクトリ（RestoreFiles=true の場合のみ）
        if (this.Options.RestoreFiles) {
            var filesDir = Path.Combine(root.FullName, "files");
            if (!Directory.Exists(filesDir)) {
                throw new DirectoryNotFoundException($"files ディレクトリが見つかりません: {filesDir}");
            }

            // manifest.FileCount が 0 でない場合は最低限の存在チェック
            if (manifest.FileCount > 0) {
                var recordDirs = Directory.GetDirectories(filesDir);
                if (recordDirs.Length == 0) {
                    throw new DirectoryNotFoundException("files ディレクトリにレコード別フォルダが存在しません。");
                }
            }
        }

        // 6) AppId の一致チェックは「コピー用途」を考慮して行わない
        //    → RestoreRequest.AppId と manifest.AppId が異なっても良い
        //    → ただし fields.json のスキーマ検証は別途 ValidateSchemaAsync で行う

        this._logger?.LogInformation("バックアップディレクトリ検証 OK");
    }

    /// <summary>
    /// スキーマ検証を実行します
    /// </summary>
    /// <param name="backupSchema">バックアップのスキーマ情報</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">スキーマ検証に失敗した場合にスローされます</exception>
    private async Task ValidateSchemaAsync(JsonNode backupSchema) {
        if (this.Options.Force) {
            this._logger?.LogWarning("Force オプションが指定されているため、スキーマ検証をスキップします");
            return;
        }

        // リストア先アプリのスキーマを取得
        var access = this._accessFactory.CreateApiTokenAccess(this.Options.SubDomain, this.Options.ApiToken);
        var currentSchema = await this._metadataApi.GetAppMetadataAsync(
            access.Domain,
            this.Options.ApiToken,
            this.Options.AppId
        );

        // バックアップ側のフィールド一覧を取得
        var properties = backupSchema["properties"]?.AsObject()
            ?? throw new InvalidOperationException("fields.json に 'properties' が存在しません。");

        foreach (var kv in properties) {
            var fieldCode = kv.Key;
            var backupFieldObj = kv.Value!.AsObject();

            // フィールドタイプの一致チェック
            var backupType = backupFieldObj["type"]?.ToString()
                ?? throw new InvalidOperationException($"fields.json の '{fieldCode}' に type がありません。");

            // Backup プロジェクトで扱わないフィールドタイプはスキップ
            if (!KintoneFieldTypeMapper.TryConvert(backupType, out _)) { continue; }

            // リストア先にフィールドが存在するか
            var currentField = currentSchema.Fields.FirstOrDefault(f => f.FieldCode == fieldCode)
                ?? throw new InvalidOperationException($"リストア先アプリにフィールド '{fieldCode}' が存在しません。");

            var currentType = currentField.OriginalFieldType;

            if (!string.Equals(backupType, currentType, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException(
                    $"フィールド '{fieldCode}' の型が一致しません。バックアップ: {backupType}, リストア先: {currentType}"
                );
            }

            // FILE フィールドは型一致だけで十分
            if (backupType == "FILE") { continue; }

            // 追加の型固有チェックが必要ならここに追加可能
        }

        this._logger?.LogInformation("スキーマ検証 OK: バックアップのフィールドはすべてリストア先に存在します");
    }

    /// <summary>
    /// 既存レコードを全削除します
    /// </summary>
    /// <param name="result">リストア結果</param>
    private async Task DeleteAllRecordsAsync(RestoreResult result) {
        if (this.Options.DryRun) {
            this._logger?.LogWarning("DryRun: 全レコード削除が実行される予定です（実際には削除されません）");
            return;
        }

        this._logger?.LogInformation("既存レコードの削除を開始します…");

        var batch = new List<int>(KintoneDeleteLimit);

        await foreach (var record in this._api!.StreamRecordsAsync("", fields: ["$id"])) {
            if (!record.TryGetProperty("$id", out var idProp)
                || !idProp.TryGetProperty("value", out var valueProp)) { continue; }

            var id = valueProp.GetString();
            if (id is null) { continue; }

            batch.Add(int.Parse(id));

            if (batch.Count >= KintoneDeleteLimit) {
                await this.FlushDeleteBatchAsync(batch, result);
                batch.Clear();
            }
        }

        if (batch.Count > 0) {
            await this.FlushDeleteBatchAsync(batch, result);
        }

        if (result.DeletedRecords == 0) {
            this._logger?.LogInformation("削除対象のレコードはありませんでした。");
        } else {
            this._logger?.LogInformation("既存レコードの削除が完了しました({Count}件)", result.DeletedRecords);
        }
    }

    /// <summary>
    /// レコード削除バッチを実行します
    /// </summary>
    /// <param name="batch">削除するレコード Id のリスト</param>
    /// <param name="result">リストア結果</param>
    private async Task FlushDeleteBatchAsync(List<int> batch, RestoreResult result) {
        var deleteJson = new JsonObject {
            ["app"] = this.Options.AppId,
            ["ids"] = new JsonArray(batch.Select(id => JsonValue.Create(id)).ToArray())
        };

        try {
            await this._api!.RawDeleteAsync(deleteJson.ToJsonString());
            this._logger?.LogInformation("{Count} 件のレコードを削除しました", batch.Count);
            result.DeletedRecords += batch.Count;
        } catch (Exception ex) {
            this._logger?.LogError(ex, "レコード削除中にエラーが発生しました（Id: {Ids}）", string.Join(",", batch));
            throw;
        }
    }
}
