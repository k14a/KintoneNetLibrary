using System.Text.Json;
using System.Text.Json.Nodes;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Backup.Application.DTOs;
using KintoneNetLibrary.Backup.Application.Interfaces;
using KintoneNetLibrary.Backup.Domain.Enums;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using Microsoft.Extensions.Logging;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;

namespace KintoneNetLibrary.Backup.Infrastructure.Services;

/// <summary>
/// リストアサービス
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
/// <param name="schemaProvider"></param>
/// <param name="httpClient"></param>
/// <param name="logger"></param>
public sealed class RestoreService(ISchemaProvider schemaProvider, IKintoneAppMetadataApi metadataApi, HttpClient? httpClient = null, ILogger<RestoreService>? logger = null) : IRestoreService {
    private IKintoneApi? _api;
    private readonly IKintoneAppMetadataApi _metadataApi = metadataApi;
    private readonly ISchemaProvider _schemaProvider = schemaProvider;
    private HttpClient? _httpClient = httpClient;
    private readonly ILogger? _logger = logger;
    private readonly RestoreResult _result = new();
    public RestoreOptions Options { get; set; } = default!;

    private void EnsureApiInitialized() {
        if (this._api != null) { return; }
        ArgumentNullException.ThrowIfNull(this.Options);
        var access = new ApiTokenAccess(this.Options.SubDomain, this.Options.ApiToken);

        this._httpClient ??= new HttpClient {
            BaseAddress = new Uri($"https://{this.Options.SubDomain}/k/v1/")
        };

        this._api = new KintoneApi(access: access, appID: this.Options.AppID, httpClient: this._httpClient);
    }

    /// <summary>
    /// リストアを実行します
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<RestoreResult> RunRestoreAsync() {
        this._logger?.LogInformation("リストア 開始: App={App}", this.Options.AppID);
        this.EnsureApiInitialized();

        try {
            // 1) バックアップディレクトリ検証
            var manifest = await this.LoadManifestAsync();
            var schema = await this.LoadFieldSchemaAsync();
            this.ValidateBackupDirectory(manifest);

            // 2) スキーマ検証(Force オプションが false の場合)
            if (!this.Options.Force) {
                await this.ValidateSchemaAsync(schema);
            }

            // 3) RestoreMode に応じた前処理
            switch (this.Options.Mode) {
                case RestoreMode.FullReplace:
                    await this.DeleteAllRecordsAsync();
                    break;
                case RestoreMode.Merge:
                    // 既存レコードは保持、新規のみ追加
                    break;
                case RestoreMode.Upsert:
                    // $id が存在する場合は更新、存在しない場合は追加
                    break;
            }

            // 4) data/part-xxxx.json を順次読み込み、レコード復元
            var partFiles = this.GetPartFiles(manifest.Parts);

            foreach (var partFile in partFiles) {
                var records = await this.LoadPartRecordsAsync(partFile);

                // 4-1) 添付ファイルの fileKey を更新
                if (this.Options.RestoreFiles) {
                    await this.ReplaceFileKeysAsync(records);
                }

                // 4-2) レコード復元
                await this.RestoreRecordsAsync(records);
            }

            // 5) 結果を返す
            this._result.Success = true;
            return this._result;

            // // 1) backup.json 読み込み
            // var backup = await this.LoadBackupJsonAsync();

            // // 2) metadata 読み込み
            // var metadata = backup["metadata"]!;

            // // 3) fields.json 読み込み
            // var schema = await this.LoadFieldSchemaAsync(metadata);

            // // 4) 現在のスキーマと比較
            // await this.ValidateSchemaAsync(schema);

            // // 5) レコード復元
            // switch (this.Options.Mode) {
            //     case RestoreMode.FullReplace:
            //         await this.DeleteAllRecordsAsync();
            //         await this.RestoreRecordsCreateAllAsync(backup);
            //         break;
            //     case RestoreMode.Upsert:
            //         await this.RestoreRecordsUpsertAsync(backup);
            //         break;
            //     case RestoreMode.Merge:
            //         await this.RestoreRecordsCreateOnlyAsync(backup);
            //         break;
            //     default:
            //         throw new NotSupportedException($"Unsupported RestoreMode: {this.Options.Mode}");
            // }

            // // 6) 添付ファイル復元
            // if (this.Options.RestoreFiles) {
            //     await this.RestoreFilesAsync(backup, metadata);
            // }

            // return this._result;

        } catch (Exception ex) {
            this._logger?.LogError(ex, "リストア 中にエラーが発生しました");
            this._result.Success = false;
            this._result.Errors.Add(ex.Message);
            return this._result;

        } finally {
            this._logger?.LogInformation("リストア 完了");
        }
    }

    /// <summary>
    /// manifest.json を読み込みます
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<BackupManifest> LoadManifestAsync() {
        var path = Path.Combine(this.Options.BackupRootPath.FullName, "manifest.json");
        if (!File.Exists(path)) {
            throw new FileNotFoundException("manifest.json が見つかりません", path);
        }
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<BackupManifest>(json)!;
    }

    /// <summary>
    /// fields.json を読み込みます
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    private async Task<JsonElement> LoadFieldSchemaAsync() {
        var path = Path.Combine(this.Options.BackupRootPath.FullName, "fields.json");
        if (!File.Exists(path)) {
            throw new FileNotFoundException("fields.json が見つかりません", path);
        }
        var json = await File.ReadAllTextAsync(path);
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// バックアップデータを読み込みます
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private IEnumerable<string> GetPartFiles(int parts) {
        var dataDir = Path.Combine(this.Options.BackupRootPath.FullName, "data");

        for (var i = 1; i <= parts; i++) {
            var file = Path.Combine(dataDir, $"part-{i:D4}.json");
            if (!File.Exists(file)) {
                throw new FileNotFoundException("バックアップデータファイルが見つかりません", file);
            }
            yield return file;
        }
    }

    /// <summary>
    /// data/part-xxxx.json を読み込みます
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private async Task<List<JsonElement>> LoadPartRecordsAsync(string path) {
        var json = await File.ReadAllTextAsync(path);
        using var doc = JsonDocument.Parse(json);
        return [.. doc.RootElement.GetProperty("records").EnumerateArray()];
    }

    /// <summary>
    /// 添付ファイルの fileKey を置換します
    /// </summary>
    /// <param name="records"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<List<JsonNode>> ReplaceFileKeysAsync(List<JsonElement> records) {
        var updatedRecords = new List<JsonNode>();

        foreach (var record in records) {
            // JsonElement → JsonNode に変換
            var node = JsonNode.Parse(record.GetRawText())!.AsObject();

            // レコード番号 ($id) を取得
            var recordId = (node["$id"]?["value"]?.ToString()) ?? throw new InvalidOperationException("レコードに $id が存在しません。");

            foreach (var field in node) {
                var fieldCode = field.Key; // FILE フィールドのコード
                var fieldObj = field.Value?.AsObject();
                if (fieldObj is null) { continue; }

                // FILE フィールドのみ対象
                if (fieldObj["type"]?.ToString() != "FILE") { continue; }

                var fileArray = fieldObj["value"]?.AsArray();
                if (fileArray is null) { continue; }

                for (int i = 0; i < fileArray.Count; i++) {
                    var fileNode = fileArray[i]!.AsObject();

                    var fileName = fileNode["name"]!.ToString();

                    // 新しい構造に対応したローカルパス
                    var localPath = Path.Combine(
                        this.Options.BackupRootPath.FullName,
                        "files",
                        recordId,
                        fieldCode,
                        fileName
                    );

                    // 新しい fileKey を取得
                    var fi = new FileInfo(localPath);
                    var newFileKey = await this._api.UploadFileAsync(fi.OpenRead(), fileName);

                    // JSON を書き換え
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
    /// <param name="records"></param>
    /// <returns></returns>
    private async Task RestoreRecordsAsync(List<JsonNode> records) {
        switch (this.Options.Mode) {
            case RestoreMode.FullReplace:
                await this.RestoreRecordsCreateOnlyAsync(records);
                break;
            case RestoreMode.Merge:
                await this.RestoreRecordsCreateOnlyAsync(records, true);
                break;
            case RestoreMode.Upsert:
                await this.RestoreRecordsUpsertAsync(records);
                break;
            default:
                throw new NotSupportedException($"Unsupported RestoreMode: {this.Options.Mode}");
        }
    }

    /// <summary>
    /// レコードを一括インサートします
    /// </summary>
    /// <param name="records"></param>
    /// <param name="skipExisting"></param>
    /// <returns></returns>
    private async Task RestoreRecordsCreateOnlyAsync(List<JsonNode> records, bool skipExisting = false) {
        var batch = new List<JsonNode>(this.Options.BatchSize);

        foreach (var record in records) {
            var id = record["$id"]?["value"]?.ToString();

            if (skipExisting && id is not null) {
                // 既存レコードはスキップ（Merge モード）
                continue;
            }

            // $id と $revision を削除して新規作成にする
            record.AsObject().Remove("$id");
            record.AsObject().Remove("$revision");

            batch.Add(record);

            // バッチサイズに達したら一括登録
            if (batch.Count >= this.Options.BatchSize) {
                await this.InsertBatchAsync(batch);
                batch.Clear();
            }
        }
        if (batch.Count > 0) {
            await this.InsertBatchAsync(batch);
        }
    }

    private async Task RestoreRecordsUpsertAsync(List<JsonNode> records) {
        var postBatch = new List<JsonNode>(this.Options.BatchSize);
        var putBatch = new List<JsonNode>(this.Options.BatchSize);

        foreach (var record in records) {
            var id = record["$id"]?["value"]?.ToString();

            if (id is null) {
                // POST(新規作成)
                var newRecord = record.DeepClone().AsObject();
                newRecord.Remove("$id");
                newRecord.Remove("$revision");
                postBatch.Add(newRecord);
                if (postBatch.Count >= this.Options.BatchSize) {
                    await this.InsertBatchAsync(postBatch);
                    postBatch.Clear();
                }
            } else {
                // PUT(更新)
                var updateRecord = record.DeepClone().AsObject();
                updateRecord.Remove("$revision");

                var wrapper = new JsonObject {
                    ["id"] = id,
                    ["record"] = updateRecord
                };
                putBatch.Add(wrapper);

                if (putBatch.Count >= this.Options.BatchSize) {
                    await this.UpdateBatchAsync(putBatch);
                    putBatch.Clear();
                }
            }
        }

        // 端数処理
        if (postBatch.Count > 0) {
            await this.InsertBatchAsync(postBatch);
        }
        if (putBatch.Count > 0) {
            await this.UpdateBatchAsync(putBatch);
        }
    }

    /// <summary>
    /// レコードを一括登録します
    /// </summary>
    /// <param name="batch"></param>
    /// <returns></returns>
    private async Task InsertBatchAsync(List<JsonNode> batch) {
        if (batch.Count == 0) { return; }

        var root = new JsonObject {
            ["records"] = new JsonArray(batch.ToArray())
        };

        var json = root.ToJsonString();
        await this._api!.RawCreateAsync(json);
        this._result.AddedRecords += batch.Count;
    }

    /// <summary>
    /// レコードを一括更新します
    /// </summary>
    /// <param name="batch"></param>
    /// <returns></returns>
    private async Task UpdateBatchAsync(List<JsonNode> batch) {
        if (batch.Count == 0) { return; }

        var root = new JsonObject {
            ["records"] = new JsonArray(batch.ToArray())
        };

        await this._api!.RawUpdateAsync(root.ToJsonString());

        this._result.UpdatedRecords += batch.Count;
    }

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
        for (int i = 1; i <= manifest.Parts; i++) {
            var partPath = Path.Combine(dataDir, $"part-{i:D4}.json");
            if (!File.Exists(partPath)) {
                throw new FileNotFoundException($"分割ファイルが不足しています: {partPath}");
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

    private async Task ValidateSchemaAsync(JsonElement backupSchema) {
        if (this.Options.Force) {
            this._logger?.LogWarning("Force オプションが指定されているため、スキーマ検証をスキップします");
            return;
        }

        // リストア先アプリのスキーマを取得
        var currentSchema = await this._metadataApi.GetAppMetadataAsync(this.Options.AppID, this.Options.ApiToken);

        // バックアップ側のフィールド一覧
        var backupFields = backupSchema.GetProperty("properties").EnumerateObject();

        foreach (var backupField in backupFields) {
            var fieldCode = backupField.Name;
            var backupFieldObj = backupField.Value;

            // リストア先にフィールドが存在するか
            if (!currentSchema.Properties.TryGetProperty(fieldCode, out var currentFieldObj)) {
                throw new InvalidOperationException(
                    $"リストア先アプリにフィールド '{fieldCode}' が存在しません。"
                );
            }

            // フィールドタイプの一致チェック
            var backupType = backupFieldObj.GetProperty("type").GetString();
            var currentType = currentFieldObj.GetProperty("type").GetString();

            if (!string.Equals(backupType, currentType, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException(
                    $"フィールド '{fieldCode}' の型が一致しません。バックアップ: {backupType}, リストア先: {currentType}"
                );
            }

            // FILE フィールドの場合は特別扱い（構造が複雑なため）
            if (backupType == "FILE") {
                // FILE フィールドは型一致だけで十分
                continue;
            }

            // 追加の型固有チェックが必要ならここに追加可能
        }

        this._logger?.LogInformation("スキーマ検証 OK: バックアップのフィールドはすべてリストア先に存在します");
    }

    // /// <summary>
    // /// スキーマを検証します
    // /// </summary>
    // /// <param name="backupSchemaJson"></param>
    // /// <returns></returns>
    // /// <exception cref="InvalidOperationException"></exception>
    // private async Task ValidateSchemaAsync(JsonNode backupSchemaJson) {
    //     if (this.Options.Force) {
    //         this._logger?.LogWarning("Force オプションによりスキーマチェックをスキップします");
    //         return;
    //     }

    //     var backupSchema = JsonSerializer.Deserialize<KintoneAppMetadata>(backupSchemaJson);

    //     var diffs = await this._schemaProvider.CompareAsync(
    //         backupSchema!,
    //         this.Options.AppID,
    //         this.Options.ApiToken
    //     );

    //     if (diffs.Count > 0) {
    //         this._logger?.LogError("スキーマ差分が検出されました。復元を中止します。");
    //         foreach (var diff in diffs) {
    //             this._logger?.LogError(diff.ToString());
    //         }
    //         throw new InvalidOperationException("スキーマが一致しません。");
    //     }

    //     this._logger?.LogInformation("スキーマ一致: 復元を続行します");
    // }







    /// <summary>
    /// バックアップ JSON を読み込みます
    /// </summary>
    /// <returns></returns>
    private async Task<JsonNode> LoadBackupJsonAsync() {
        var json = await File.ReadAllTextAsync(this.Options.BackupJsonPath);
        return JsonNode.Parse(json)!;
    }

    /// <summary>
    /// フィールドスキーマを読み込みます
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    private async Task<JsonNode> LoadFieldSchemaAsync(JsonNode metadata) {
        var dir = Path.GetDirectoryName(this.Options.BackupJsonPath)!;
        var schemaFile = metadata["fieldSchemaFile"]!.ToString();
        var json = await File.ReadAllTextAsync(Path.Combine(dir, schemaFile));
        return JsonNode.Parse(json)!;
    }



    /// <summary>
    /// 既存レコードを全削除します
    /// </summary>
    /// <returns></returns>
    private async Task DeleteAllRecordsAsync() {
        this._logger?.LogInformation("既存レコードの削除を開始します…");

        // 1) まず全レコードの ID を取得する
        var idList = new List<string>();

        // RawFindAllAsync を使って全件取得（ID のみ）
        var json = await this._api.RawFindAllAsync(fieldCodes: new[] { "レコード番号" });

        if (json is null) {
            this._logger?.LogWarning("レコードが取得できませんでした。削除をスキップします。");
            return;
        }

        var root = JsonNode.Parse(json)!;
        var records = root["records"]!.AsArray();

        foreach (var record in records) {
            var id = record?["レコード番号"]?["value"]?.ToString();
            if (id is not null) { idList.Add(id); }
        }

        if (idList.Count == 0) {
            this._logger?.LogInformation("削除対象のレコードはありませんでした。");
            return;
        }

        this._logger?.LogInformation("削除対象レコード数: {Count}", idList.Count);

        // 2) 100件ずつ削除
        for (int i = 0; i < idList.Count; i += KintoneDeleteLimit) {
            var batch = idList
                .Skip(i)
                .Take(KintoneDeleteLimit)
                .ToArray();

            var deleteJson = new JsonObject {
                ["ids"] = new JsonArray(batch.Select(id => JsonValue.Create(id)).ToArray())
            };

            try {
                await this._api.RawDeleteAsync(deleteJson.ToJsonString());
                this._logger?.LogInformation("{Count} 件のレコードを削除しました", batch.Length);
                this._result.DeletedRecords += batch.Length;

            } catch (Exception ex) {
                this._logger?.LogError(ex, "レコード削除中にエラーが発生しました（ID: {Ids}）", string.Join(",", batch));
                throw;
            }
        }

        this._logger?.LogInformation("既存レコードの削除が完了しました({Count}件)", this._result.DeletedRecords);
    }

    /// <summary>
    /// 添付ファイル復元を実行します
    /// </summary>
    /// <param name="backupJson"></param>
    /// <param name="metadataJson"></param>
    /// <returns></returns>
    private async Task RestoreFilesAsync(JsonNode backupJson, JsonNode metadataJson) {
        this._logger?.LogInformation("添付ファイル復元を開始します…");

        var records = backupJson["records"]!.AsArray();

        var filesRoot = Path.Combine(
            Path.GetDirectoryName(this.Options.BackupJsonPath)!,
            metadataJson["fileDirectory"]!.ToString()
        );

        foreach (var recordNode in records) {
            if (recordNode is not JsonObject record) { continue; }

            // レコード番号を取得
            var recordId = record["レコード番号"]?["value"]?.ToString();
            if (recordId is null) {
                this._logger?.LogWarning("レコード番号が見つからないレコードがありました。スキップします。");
                continue;
            }

            var recordDir = Path.Combine(filesRoot, recordId);
            if (!Directory.Exists(recordDir)) {
                this._logger?.LogInformation("レコード {RecordId} に添付ファイルはありません", recordId);
                continue;
            }

            // FILE フィールドを探す
            foreach (var field in record) {
                if (field.Value is not JsonObject fieldObj) { continue; }

                if (fieldObj["type"]?.ToString() != "FILE") { continue; }

                if (fieldObj["value"] is not JsonArray fileArray) { continue; }

                for (int i = 0; i < fileArray.Count; i++) {
                    if (fileArray[i] is not JsonObject fileInfo) { continue; }

                    var fileName = fileInfo["name"]?.ToString();
                    if (fileName is null) { continue; }

                    var localPath = Path.Combine(recordDir, fileName);

                    if (!File.Exists(localPath)) {
                        this._logger?.LogWarning("ファイルが見つかりません: {Path}", localPath);
                        continue;
                    }

                    this._logger?.LogInformation("Uploading file: {FileName}", fileName);

                    // 新しい fileKey を取得
                    var fi = new FileInfo(localPath);
                    var newFileKey = await this._api.UploadFileAsync(fi.OpenRead(), fileName);

                    // JSON の fileKey を置き換える
                    fileInfo["fileKey"] = newFileKey;
                    this._result.UploadedFiles++;
                }
            }

            // レコードを RawUpdateAsync で更新
            var updateJson = new JsonObject {
                ["id"] = recordId,
                ["record"] = record
            };

            await this._api.RawUpdateAsync(updateJson.ToJsonString());

            this._logger?.LogInformation("レコード {RecordId} の添付ファイルを更新しました", recordId);
        }

        this._logger?.LogInformation("添付ファイル復元が完了しました({Count}件)", this._result.UploadedFiles);
    }


    /// <summary>
    /// Merge モードでレコード追加を実行します
    /// </summary>
    /// <param name="backupJson"></param>
    /// <returns></returns>
    private async Task RestoreRecordsCreateOnlyAsync(JsonNode backupJson) {
        this._logger?.LogInformation("Merge モードでレコード追加を開始します…");

        var records = backupJson["records"]!.AsArray();

        if (records.Count == 0) {
            this._logger?.LogWarning("バックアップにレコードが含まれていません。追加をスキップします。");
            return;
        }

        // RawCreateAsync に渡す JSON を構築
        var createJson = new JsonObject { ["records"] = records };

        try {
            await this._api.RawCreateAsync(createJson.ToJsonString());
            this._logger?.LogInformation("{Count} 件のレコードを追加しました", records.Count);
            this._result.AddedRecords = records.Count;

        } catch (Exception ex) {
            this._logger?.LogError(ex, "レコード追加中にエラーが発生しました");
            throw;
        }

        this._logger?.LogInformation("Merge モードでのレコード追加が完了しました");
    }

    /// <summary>
    /// レコードの存在確認を行います
    /// </summary>
    /// <param name="recordId"></param>
    /// <returns></returns>
    private async Task<bool> RecordExistsAsync(string recordId) {
        var query = $"レコード番号 = {recordId}";
        var json = await this._api.RawFindByQueryAsync(query, fieldCodes: new[] { "レコード番号" });

        if (json is null) { return false; }

        var root = JsonNode.Parse(json)!;
        var count = root["records"]!.AsArray().Count;

        return count > 0;
    }

    /// <summary>
    /// レコードを更新します
    /// </summary>
    /// <param name="recordId"></param>
    /// <param name="record"></param>
    /// <returns></returns>
    private async Task UpdateRecordAsync(string recordId, JsonObject record) {
        this._logger?.LogInformation("レコード更新: ID={RecordId}", recordId);

        var updateJson = new JsonObject {
            ["id"] = recordId,
            ["record"] = record
        };

        await this._api.RawUpdateAsync(updateJson.ToJsonString());
    }

    /// <summary>
    /// レコードを新規作成します
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private async Task CreateRecordAsync(JsonObject record) {
        this._logger?.LogInformation("レコード新規作成");

        var createJson = new JsonObject {
            ["record"] = record
        };

        await this._api.RawCreateAsync(createJson.ToJsonString());
    }

}
