using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.CodeGen.Application.Interfaces;
using KintoneNetLibrary.Domain.Access;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Backup.Application.DTOs;
using KintoneNetLibrary.Backup.Domain.Enums;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;

namespace KintoneNetLibrary.Backup.Infrastructure.Services;

/// <summary>
/// リストアサービス
/// </summary>
public sealed class RestoreService {
    private readonly RestoreOptions _options;
    private readonly IKintoneApi _api;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ILogger? _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="options"></param>
    /// <param name="schemaProvider"></param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public RestoreService(
        RestoreOptions options,
        ISchemaProvider schemaProvider,
        HttpClient? httpClient = null,
        ILogger<RestoreService>? logger = null) {
        ArgumentNullException.ThrowIfNull(options);

        this._options = options;
        this._logger = logger;
        this._schemaProvider = schemaProvider;

        var access = new ApiTokenAccess(options.SubDomain, options.ApiToken);

        httpClient ??= new HttpClient {
            BaseAddress = new Uri($"https://{options.SubDomain}/k/v1/")
        };

        this._api = new KintoneApi(access: access, appID: options.AppID, httpClient: httpClient);
    }

    /// <summary>
    /// リストアを実行します
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task RunRestoreAsync() {
        this._logger?.LogInformation("Restore 開始: App={App}", this._options.AppID);

        // 1) backup.json 読み込み
        var backup = await this.LoadBackupJsonAsync();

        // 2) metadata 読み込み
        var metadata = backup["metadata"]!;

        // 3) fields.json 読み込み
        var schema = await this.LoadFieldSchemaAsync(metadata);

        // 4) 現在のスキーマと比較
        await this.ValidateSchemaAsync(schema);

        // 5) レコード復元
        switch (this._options.Mode) {
            case RestoreMode.FullReplace:
                await this.DeleteAllRecordsAsync();
                await this.RestoreRecordsCreateAllAsync(backup);
                break;
            case RestoreMode.Upsert:
                await this.RestoreRecordsUpsertAsync(backup);
                break;
            case RestoreMode.Merge:
                await this.RestoreRecordsCreateOnlyAsync(backup);
                break;
            default:
                throw new NotSupportedException($"Unsupported RestoreMode: {this._options.Mode}");
        }

        // 6) 添付ファイル復元
        if (this._options.RestoreFiles) {
            await this.RestoreFilesAsync(backup, metadata);
        }

        this._logger?.LogInformation("Restore 完了");
    }

    /// <summary>
    /// バックアップ JSON を読み込みます
    /// </summary>
    /// <returns></returns>
    private async Task<JsonNode> LoadBackupJsonAsync() {
        var json = await File.ReadAllTextAsync(this._options.BackupJsonPath);
        return JsonNode.Parse(json)!;
    }

    /// <summary>
    /// フィールドスキーマを読み込みます
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    private async Task<JsonNode> LoadFieldSchemaAsync(JsonNode metadata) {
        var dir = Path.GetDirectoryName(_options.BackupJsonPath)!;
        var schemaFile = metadata["fieldSchemaFile"]!.ToString();
        var json = await File.ReadAllTextAsync(Path.Combine(dir, schemaFile));
        return JsonNode.Parse(json)!;
    }

    /// <summary>
    /// スキーマを検証します
    /// </summary>
    /// <param name="backupSchemaJson"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task ValidateSchemaAsync(JsonNode backupSchemaJson) {
        if (this._options.Force) {
            this._logger?.LogWarning("Force オプションによりスキーマチェックをスキップします");
            return;
        }

        var backupSchema = JsonSerializer.Deserialize<KintoneAppMetadata>(backupSchemaJson);

        var diffs = await this._schemaProvider.CompareAsync(
            backupSchema!,
            this._options.AppID,
            this._options.ApiToken
        );

        if (diffs.Count > 0) {
            this._logger?.LogError("スキーマ差分が検出されました。復元を中止します。");
            foreach (var diff in diffs) {
                this._logger?.LogError(diff.ToString());
            }
            throw new InvalidOperationException("スキーマが一致しません。");
        }

        this._logger?.LogInformation("スキーマ一致: 復元を続行します");
    }

    /// <summary>
    /// FullReplace モードでレコード復元を実行します
    /// </summary>
    /// <param name="backupJson"></param>
    /// <returns></returns>
    private async Task RestoreRecordsCreateAllAsync(JsonNode backupJson) {
        this._logger?.LogInformation("FullReplace モードでレコード復元を開始します…");

        // 1) backup.json の records を取得
        var records = backupJson["records"]!.AsArray();

        if (records.Count == 0) {
            this._logger?.LogWarning("バックアップにレコードが含まれていません。復元をスキップします。");
            return;
        }

        // 2) RawCreateAsync に渡す JSON を構築
        var createJson = new JsonObject {
            ["records"] = records
        };

        // 3) 一括登録
        try {
            await this._api.RawCreateAsync(createJson.ToJsonString());
            this._logger?.LogInformation("{Count} 件のレコードを新規作成しました", records.Count);
        } catch (Exception ex) {
            this._logger?.LogError(ex, "レコードの一括作成中にエラーが発生しました");
            throw;
        }

        this._logger?.LogInformation("FullReplace モードでのレコード復元が完了しました");
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

            } catch (Exception ex) {
                this._logger?.LogError(ex, "レコード削除中にエラーが発生しました（ID: {Ids}）", string.Join(",", batch));
                throw;
            }
        }

        this._logger?.LogInformation("既存レコードの削除が完了しました");
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
            Path.GetDirectoryName(this._options.BackupJsonPath)!,
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

        this._logger?.LogInformation("添付ファイル復元が完了しました");
    }

    /// <summary>
    /// Upsert モードでレコード復元を実行します
    /// </summary>
    /// <param name="backupJson"></param>
    /// <returns></returns>
    private async Task RestoreRecordsUpsertAsync(JsonNode backupJson) {
        this._logger?.LogInformation("Upsert モードでレコード復元を開始します…");

        var records = backupJson["records"]!.AsArray();

        foreach (var recordNode in records) {
            if (recordNode is not JsonObject record) {
                continue;
            }

            // レコード番号を取得
            var recordId = record["レコード番号"]?["value"]?.ToString();

            if (string.IsNullOrEmpty(recordId)) {
                this._logger?.LogWarning("レコード番号が無いため、新規作成として扱います");
                await this.CreateRecordAsync(record);
                continue;
            }

            // 既存レコードの存在確認
            var exists = await this.RecordExistsAsync(recordId);

            if (exists) {
                await this.UpdateRecordAsync(recordId, record);
            } else {
                await this.CreateRecordAsync(record);
            }
        }

        this._logger?.LogInformation("Upsert モードでのレコード復元が完了しました");
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
