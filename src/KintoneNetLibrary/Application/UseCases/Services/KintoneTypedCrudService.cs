using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Helpers;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KintoneNetLibrary.Application.Interfaces;
using System.Reflection;

namespace KintoneNetLibrary.Application.UseCases.Services;
/// <summary>
/// KintoneモデルのCRUD操作を提供するサービスクラスです。
/// </summary>
/// <param name="repository">Kintoneリポジトリインターフェース</param>
/// <param name="executionOptions">Kintone実行オプション</param>
/// <param name="jsonOptions">JSONシリアライズオプション（オプション）</param>
/// <param name="logger">ロガーインスタンス（オプション）</param>
public class KintoneTypedCrudService<T>(
    IKintoneRepository repository,
    IOptions<KintoneExecutionOptions>? executionOptions,
    JsonSerializerOptions? jsonOptions = null,
    ILogger<KintoneTypedCrudService<T>>? logger = null) : IKintoneTypedCrudService<T> where T : KintoneModelBase<T>, new() {

    private readonly IKintoneRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<IKintoneTypedCrudService<T>>? _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions ?? DefaultJsonOptions.Default;
    private readonly KintoneExecutionOptions _execOptions = executionOptions?.Value ?? new KintoneExecutionOptions();

    /// <summary>
    /// Kintoneモデルのレコードを作成します。
    /// </summary>
    /// <param name="records">作成対象のKintoneモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一レコードの再試行を有効にするかどうか</param>
    /// <returns>作成結果を含むKintoneWriteResultオブジェクト</returns>
    public async Task<KintoneWriteResult<T>> CreateAsync(IList<T> records, bool enableSingleRetryOnError = false) {
        try {
            this._logger?.LogInformation("CreateAsync() - Start");

            var result = new KintoneWriteResult<T>();
            var chunks = records.Chunk(KintoneLimit).ToList();
            var semaphore = new SemaphoreSlim(this._execOptions.MaxConcurrency);

            var tasks = chunks.Select(async chunk => {
                await semaphore.WaitAsync();

                try {
                    var partialResult = await this.CreateChunkAsync(chunk.ToList(), enableSingleRetryOnError);
                    lock (result) {
                        result.Succeeded.AddRange(partialResult.Succeeded);
                        result.Failed.AddRange(partialResult.Failed);
                    }
                } finally {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            return result;

        } finally {
            this._logger?.LogInformation("CreateAsync() - Finish");
        }
    }

    /// <summary>
    /// 指定されたチャンクのレコードをKintoneアプリに作成します。
    /// </summary>
    /// <param name="chunk">作成対象のKintoneモデルのチャンク</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一レコードの再試行を有効にするかどうか</param>
    /// <returns>作成結果を含むKintoneWriteResultオブジェクト</returns>
    private async Task<KintoneWriteResult<T>> CreateChunkAsync(IList<T> chunk, bool enableSingleRetryOnError) {
        var result = new KintoneWriteResult<T>();

        try {
            var responseJson = await this._repository.CreateRecordsAsync(chunk);
            var parsed = KintoneResponseParser.ParseCreatedRecords(chunk, responseJson);
            result.Succeeded.AddRange(parsed);

        } catch (KintoneException ex) {
            this._logger?.LogWarning("Bulk insert failed: {Summary}", ex.Message);

            if (!enableSingleRetryOnError) {
                foreach (var record in chunk) {
                    result.Failed.Add(new KintoneWriteFailure<T> {
                        Record = record,
                        ErrorMessage = ex.Message,
                        Error = ex.Error
                    });
                }
                return result;
            }

            foreach (var record in chunk) {
                try {
                    var singleRespJson = await this._repository.CreateRecordsAsync([record]);
                    var parsed = KintoneResponseParser.ParseCreatedRecords([record], singleRespJson);
                    result.Succeeded.AddRange(parsed);

                } catch (KintoneException singleEx) {
                    this._logger?.LogError("Single insert failed: {Summary} - Record: {Record}", singleEx.Message, record);
                    result.Failed.Add(new KintoneWriteFailure<T> {
                        Record = record,
                        ErrorMessage = singleEx.Message,
                        Error = singleEx.Error
                    });
                }
            }

        } catch (Exception ex) {
            this._logger?.LogError(ex, "Unexpected error during bulk insert.");

            foreach (var record in chunk) {
                result.Failed.Add(new KintoneWriteFailure<T> {
                    Record = record,
                    ErrorMessage = ex.Message,
                    Error = null // 汎用例外なのでKintoneErrorは取れない
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Kintoneモデルのレコードを検索します。
    /// </summary>
    /// <param name="ids">検索対象のレコードIDのリスト（オプション）</param>
    /// <param name="query">検索クエリ文字列（オプション）</param>
    /// <param name="kintoneQuery">型安全なクエリビルダー（オプション）</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト（オプション）</param>
    /// <returns>検索結果のKintoneモデルのリスト</returns>
    /// <exception cref="KintoneException"></exception>
    public async Task<IEnumerable<T>> FindAsync(IList<string>? ids = null, string? query = null, KintoneQuery<T>? kintoneQuery = null, IList<string>? fieldCodes = null) {
        try {
            this._logger?.LogInformation("FindAsync() - Start");

            T model = new();

            if (ids != null && ids.Any()) {
                // IDが1件なら単一取得
                if (ids.Count == 1) {
                    var json = await this._repository.FindByIDAsync(model, ids[0]);
                    if (string.IsNullOrEmpty(json)) { return []; }

                    var record = KintoneResponseParser.ParseRecord<T>(json);
                    return [record];

                } else {
                    var json = await this._repository.FindByIDsAsync(model, ids, fieldCodes);
                    if (string.IsNullOrEmpty(json)) { return []; }

                    var records = KintoneResponseParser.ParseRecords<T>(json);
                    return records ?? [];
                }

            } else if (!string.IsNullOrEmpty(query)) {
                var json = await this._repository.FindByQueryAsync(model, query);
                if (string.IsNullOrEmpty(json)) { return []; }

                var records = KintoneResponseParser.ParseRecords<T>(json);
                return records ?? [];

            } else if (kintoneQuery != null) {
                var json = await this._repository.FindByQueryAsync(model, kintoneQuery.Build(), fieldCodes);
                if (string.IsNullOrEmpty(json)) { return []; }

                var records = KintoneResponseParser.ParseRecords<T>(json);
                return records ?? [];

            } else {
                // 全件取得
                var json = await this._repository.FindAllAsync(model, fieldCodes);
                if (string.IsNullOrEmpty(json)) { return []; }

                var records = KintoneResponseParser.ParseRecords<T>(json);
                return records ?? [];
            }

        } catch (JsonException ex) {
            this._logger?.LogError(ex, "JSON deserialization failed in FindAsync<{Model}>", typeof(T).Name);
            throw new KintoneException("Failed to parse Kintone JSON response.", ex);

        } catch (Exception ex) {
            this._logger?.LogError(ex, "Unexpected error occurred in FindAsync<{Model}>", typeof(T).Name);
            throw new KintoneException("An unexpected error occurred while retrieving Kintone records.", ex);

        } finally {
            this._logger?.LogInformation("FindAsync() - Finish");
        }
    }

    /// <summary>
    /// Kintoneモデルのレコードを更新します。
    /// </summary>
    /// <param name="records">更新対象のKintoneモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一レコードの再試行を有効にするかどうか</param>
    /// <returns>更新結果を含むKintoneWriteResultオブジェクト</returns>
    public async Task<KintoneWriteResult<T>> UpdateAsync(IList<T> records, bool enableSingleRetryOnError = false) {
        try {
            this._logger?.LogInformation("UpdateAsync() - Start");

            var result = new KintoneWriteResult<T>();
            var chunks = records.Chunk(KintoneLimit).Select(c => c.ToList()).ToList();
            var semaphore = new SemaphoreSlim(this._execOptions.MaxConcurrency);

            var tasks = chunks.Select(async chunk => {
                await semaphore.WaitAsync();
                try {
                    var partial = await this.UpdateChunkAsync(chunk, enableSingleRetryOnError);
                    lock (result) {
                        result.Succeeded.AddRange(partial.Succeeded);
                        result.Failed.AddRange(partial.Failed);
                    }
                } finally {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return result;

        } finally {
            this._logger?.LogInformation("UpdateAsync() - Finish");
        }
    }

    /// <summary>
    /// 指定されたチャンクのレコードをKintoneアプリに更新します。
    /// </summary>
    /// <param name="chunk">更新対象のKintoneモデルのチャンク</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一レコードの再試行を有効にするかどうか</param>
    /// <returns>更新結果を含むKintoneWriteResultオブジェクト</returns>
    private async Task<KintoneWriteResult<T>> UpdateChunkAsync(IList<T> chunk, bool enableSingleRetryOnError) {
        var result = new KintoneWriteResult<T>();

        try {
            var responseJson = await this._repository.UpdateRecordsAsync(chunk);
            var parsed = ParseUpdatedRecords(chunk, responseJson);
            result.Succeeded.AddRange(parsed);
        } catch (KintoneException ex) {
            this._logger?.LogWarning("Bulk update failed: {Summary}", ex.Message);

            if (!enableSingleRetryOnError) {
                foreach (var record in chunk) {
                    result.Failed.Add(new KintoneWriteFailure<T> {
                        Record = record,
                        ErrorMessage = ex.Message,
                        Error = ex.Error
                    });
                }
                return result;
            }

            // 単件リトライ
            foreach (var record in chunk) {
                try {
                    var singleRespJson = await this._repository.UpdateRecordsAsync([record]);
                    var parsed = ParseUpdatedRecords([record], singleRespJson);
                    result.Succeeded.AddRange(parsed);

                } catch (KintoneException singleEx) {
                    this._logger?.LogError("Single update failed: {Summary} - Record: {Record}", singleEx.Message, record);
                    result.Failed.Add(new KintoneWriteFailure<T> {
                        Record = record,
                        ErrorMessage = singleEx.Message,
                        Error = singleEx.Error
                    });
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <param name="ids">削除対象のレコードIDのリスト</param>
    /// <param name="validateExistence">レコードの存在確認を行うかどうか</param>
    /// <returns>削除結果を含むKintoneDeleteResultオブジェクト</returns>
    public async Task<KintoneDeleteResult> DeleteAsync(IList<string> ids, bool validateExistence = true) {
        var models = ids.Select(id => new T { RecordID = id }).ToList();
        return await this.DeleteAsync(models, validateExistence);
    }

    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <param name="models">削除対象のKintoneモデルのリスト</param>
    /// <param name="validateExistence">レコードの存在確認を行うかどうか</param>
    /// <returns>削除結果を含むKintoneDeleteResultオブジェクト</returns>
    public async Task<KintoneDeleteResult> DeleteAsync(IList<T> models, bool validateExistence = true) {
        try {
            this._logger?.LogInformation("DeleteAsync() - Start");

            if (models.Count == 0) { return new KintoneDeleteResult(); }

            foreach (var model in models) {
                await model.RunBeforeDeleteHookAsync();
            }

            var target = validateExistence ? await this.PrepareValidatedTargets(models) : models;

            var result = new KintoneDeleteResult();
            if (validateExistence) {
                var failures = KintoneTypedCrudService<T>.CollectNotFoundFailures(models, target);
                result.Failed.AddRange(failures);
            }

            var chunks = target.Chunk(KintoneDeleteLimit).Select(c => c.ToList());
            var semaphore = new SemaphoreSlim(this._execOptions.MaxConcurrency);

            var tasks = chunks.Select(async chunk => {
                await semaphore.WaitAsync();
                try {
                    var partial = await this.DeleteChunkAsync(chunk);
                    lock (result) {
                        result.Succeeded.AddRange(partial.Succeeded);
                        result.Failed.AddRange(partial.Failed);
                    }
                } finally {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return result;

        } finally {
            this._logger?.LogInformation("DeleteAsync() - Finish");
        }
    }

    /// <summary>
    /// 指定されたチャンクのレコードをKintoneアプリから削除します。
    /// </summary>
    /// <param name="chunk">削除対象のKintoneモデルのチャンク</param>
    /// <returns>削除結果を含むKintoneDeleteResultオブジェクト</returns>
    private async Task<KintoneDeleteResult> DeleteChunkAsync(IList<T> chunk) {
        var result = new KintoneDeleteResult();
        var idList = chunk.Select(m => m.RecordID).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).ToList();

        try {
            var validModels = chunk.Where(x => !string.IsNullOrWhiteSpace(x.RecordID)).ToList();
            await this._repository.DeleteRecordsAsync(validModels);
            result.Succeeded.AddRange(idList);

            foreach (var model in chunk.Where(m => idList.Contains(m.RecordID!))) {
                await model.RunAfterDeleteHookAsync();
            }

        } catch (KintoneException ex) {
            foreach (var id in idList) {
                result.Failed.Add(new KintoneDeleteFailure {
                    ID = id,
                    ErrorMessage = ex.Message,
                    Reason = KintoneDeleteFailureReason.DeleteError
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 指定されたモデルのうち、Kintoneアプリに存在するものを取得します。
    /// </summary>
    /// <param name="models">存在確認対象のKintoneモデルのリスト</param>
    /// <returns>存在するKintoneモデルのリスト</returns>
    private async Task<IList<T>> PrepareValidatedTargets(IList<T> models) {
        var ids = models.Select(x => x.RecordID).OfType<string>().ToList();
        return (await this.FindAsync(ids, fieldCodes: ["RecordID"])).ToList();
    }

    /// <summary>
    /// 指定されたオリジナルリストに対して、見つからなかったレコードの削除失敗情報を収集します。
    /// </summary>
    /// <param name="original">オリジナルのKintoneモデルのリスト</param>
    /// <param name="found">存在が確認されたKintoneモデルのリスト</param>
    /// <returns>削除失敗情報のリスト</returns>
    private static List<KintoneDeleteFailure> CollectNotFoundFailures(IList<T> original, IList<T> found) {
        var foundIds = found.Select(x => x.RecordID).ToHashSet();
        return original
            .Where(x => !foundIds.Contains(x.RecordID))
            .Select(x => new KintoneDeleteFailure {
                ID = x.RecordID ?? string.Empty,
                ErrorMessage = "Record is not found.",
                Reason = KintoneDeleteFailureReason.RecordNotFound
            }).ToList();
    }

    /// <summary>
    /// Kintoneモデルのレコードを保存します。
    /// </summary>
    /// <param name="records">保存対象のKintoneモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <returns>保存結果を含むKintoneWriteResultオブジェクト</returns>
    public async Task<KintoneWriteResult<T>> SaveAsync(IList<T> records, bool enableSingleRetryOnError = false) {
        try {
            this._logger?.LogInformation("SaveAsync() - Start");

            var result = new KintoneWriteResult<T>();

            var (createTargets, updateTargets) = await this.SplitRecordsAsync(records);

            if (createTargets.Count > 0) {
                var createResult = await this.CreateAsync(createTargets, enableSingleRetryOnError);
                result.Succeeded.AddRange(createResult.Succeeded);
                result.Failed.AddRange(createResult.Failed);
            }

            if (updateTargets.Count > 0) {
                var updateResult = await this.UpdateAsync(updateTargets, enableSingleRetryOnError);
                result.Succeeded.AddRange(updateResult.Succeeded);
                result.Failed.AddRange(updateResult.Failed);
            }

            return result;

        } finally {
            this._logger?.LogInformation("SaveAsync() - Finish");
        }
    }

    /// <summary>
    /// Kintoneモデルのレコードを保存します。作成に失敗したレコードは更新として再試行されます。
    /// </summary>
    /// <param name="records">保存対象のKintoneモデルのリスト</param>
    /// <param name="enableSingleRetryOnError">エラー発生時に単一の再試行を有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">作成に失敗したレコードを更新として再試行するかどうか</param>
    /// <returns>保存結果を含むKintoneWriteResultオブジェクト</returns>
    public async Task<KintoneWriteResult<T>> SaveWithRetryAsync(IList<T> records, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) {
        try {
            this._logger?.LogInformation("SaveWithRetryAsync() - Start");

            var result = new KintoneWriteResult<T>();

            var (createTargets, updateTargets) = await this.SplitRecordsAsync(records);

            // create 処理
            if (createTargets.Count > 0) {
                var createResult = await this.CreateAsync(createTargets, enableSingleRetryOnError);

                result.Succeeded.AddRange(createResult.Succeeded);
                result.Failed.AddRange(createResult.Failed);

                // create に失敗したレコードを update として再試行
                if (enableCreateToUpdateRetry) {
                    var retryCandidates = createResult.Failed
                        .Where(f => f.Record.HasUpdateKeyOrID()) // update できる条件を満たす
                        .Select(f => f.Record)
                        .ToList();

                    if (retryCandidates.Count != 0) {
                        var updateResult = await this.UpdateAsync(retryCandidates, enableSingleRetryOnError);
                        result.Succeeded.AddRange(updateResult.Succeeded);
                        result.Failed.RemoveAll(f => retryCandidates.Contains(f.Record)); // 一度失敗したが成功に変わったものを除外
                        result.Failed.AddRange(updateResult.Failed); // 再試行の失敗分を追加
                    }
                }
            }

            // update 処理
            if (updateTargets.Count > 0) {
                var updateResult = await this.UpdateAsync(updateTargets, enableSingleRetryOnError);
                result.Succeeded.AddRange(updateResult.Succeeded);
                result.Failed.AddRange(updateResult.Failed);
            }

            return result;

        } finally {
            this._logger?.LogInformation("SaveWithRetryAsync() - Finish");
        }
    }

    /// <summary>
    /// レスポンスJSONを解析して更新されたレコードのリストを返します。
    /// </summary>
    /// <param name="records">更新対象のKintoneモデルのリスト</param>
    /// <param name="responseJson">Kintone APIからのレスポンスJSON</param>
    /// <returns>更新されたKintoneモデルのリスト</returns>
    private static List<T> ParseUpdatedRecords(IList<T> records, string responseJson) {
        var indexResponse = KintoneRecordIndexesResponse.Parse(responseJson);

        var result = new List<T>();
        foreach (var i in Enumerable.Range(0, Math.Min(records.Count, indexResponse.Records.Count))) {
            var model = records[i];
            var indexItem = indexResponse.Records[i];
            model.RecordID = indexItem.ID ?? string.Empty;
            model.Revision = indexItem.Revision;
            result.Add(model);
        }
        return result;
    }

    /// <summary>
    /// レコードを作成対象と更新対象に分割します。
    /// </summary>
    /// <param name="records">分割対象のKintoneモデルのリスト</param>
    /// <returns>作成対象と更新対象に分割されたKintoneモデルのリスト</returns>
    private static (List<T> createTargets, List<T> updateTargets) SplitRecords(IList<T> records) {
        var createTargets = new List<T>();
        var updateTargets = new List<T>();

        foreach (var record in records) {
            if (record.HasUpdateKeyOrID()) {
                updateTargets.Add(record);
            } else {
                createTargets.Add(record);
            }
        }

        return (createTargets, updateTargets);
    }

    private async Task<(List<T> createTargets, List<T> updateTargets)> SplitRecordsAsync(IList<T> records) {
        var keyed = records.Where(r => r.HasUpdateKey()).ToList();

        if (keyed.Count == 0) {
            return (records.ToList(), new List<T>());
        }

        var keyValues = keyed
            .Select(r => r.GetUpdateKeyValue())
            .Where(v => v != null)
            .Distinct()
            .ToList();

        var keyProp = typeof(T)
            .GetProperties()
            .First(p => p.GetCustomAttribute<KintoneItemAttribute>()?.IsKey == true);

        var attr = keyProp.GetCustomAttribute<KintoneItemAttribute>();
        var keyPropName = !string.IsNullOrWhiteSpace(attr?.FieldCode)
            ? attr.FieldCode
            : keyProp.Name;

        // var query = new KintoneQuery<T>().In(keyPropName, keyValues);
        var query = CreateInQuery(keyPropName, keyValues);

        var existing = await this.FindAsync(query: query);

        var existingMap = existing.ToDictionary(
            r => r.GetUpdateKeyValue()!,
            r => (r.RecordID, r.Revision)
        );

        var createTargets = new List<T>();
        var updateTargets = new List<T>();

        foreach (var record in records) {
            var key = record.GetUpdateKeyValue();

            if (record.HasUpdateKey() &&
                key != null &&
                existingMap.TryGetValue(key, out var info)) {
                record.RecordID = info.RecordID;
                record.Revision = info.Revision;
                updateTargets.Add(record);
            } else {
                createTargets.Add(record);
            }
        }

        return (createTargets, updateTargets);
    }

    private static string CreateInQuery(string fieldName, List<string?>? values) {
        if (string.IsNullOrWhiteSpace(fieldName)) {
            throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
        }
        if (values == null || values.Count == 0) {
            throw new ArgumentException("Values collection cannot be null or empty", nameof(values));
        }

        var valueList = values.ToList();
        if (valueList.Count == -1) {
            throw new ArgumentException("Values collection cannot be empty", nameof(values));
        }

        var formattedValues = valueList.Select(v => $"\"{v}\"");
        var joinedValues = string.Join(", ", formattedValues);
        return $"{fieldName} in ({joinedValues})";
    }

}
