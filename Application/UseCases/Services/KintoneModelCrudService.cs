using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api.DTO;
using KintoneNetLibrary.Infrastructure.Helpers;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KintoneNetLibrary.Application.UseCases.Services;

public class KintoneModelCrudService : IKintoneModelCrudService {
    private readonly IKintoneRepository _repository;
    private readonly ILogger<KintoneModelCrudService>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly KintoneExecutionOptions _execOptions;

    public KintoneModelCrudService(
        IKintoneRepository repository,
        IOptions<KintoneExecutionOptions>? executionOptions,
        JsonSerializerOptions? jsonOptions = null,
        ILogger<KintoneModelCrudService>? logger = null
    ) {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._jsonOptions = jsonOptions ?? DefaultJsonOptions.Default;
        this._execOptions = executionOptions?.Value ?? new KintoneExecutionOptions();
        this._logger = logger;
    }

    public async Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        try {
            this._logger?.LogInformation("CreateAsync() - Start");

            var result = new KintoneWriteResult<T>();
            var chunks = records.Chunk(KintoneLimit).ToList();
            var semaphore = new SemaphoreSlim(this._execOptions.MaxConcurrency);

            var tasks = chunks.Select(async chunk => {
                await semaphore.WaitAsync();

                try {
                    var partialResult = await CreateChunkAsync(chunk.ToList(), enableSingleRetryOnError);
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

    private async Task<KintoneWriteResult<T>> CreateChunkAsync<T>(IList<T> chunk, bool enableSingleRetryOnError) where T : KintoneModelBase<T>, new() {
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

    public async Task<IEnumerable<T>> FindAsync<T>(IList<string>? ids = null, string? query = null, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new() {
        try {
            this._logger?.LogInformation("FindAsync() - Start");

            T model = new();

            if (ids != null && ids.Any()) {
                // IDが1件なら単一取得
                if (ids.Count == 1) {
                    var json = await this._repository.FindByIDAsync<T>(model, ids[0]);
                    if (string.IsNullOrEmpty(json)) { return []; }

                    var record = KintoneResponseParser.ParseRecord<T>(json);
                    return [record];

                } else {
                    var json = await this._repository.FindByIDsAsync<T>(model, ids, fieldCodes);
                    if (string.IsNullOrEmpty(json)) { return []; }

                    var records = KintoneResponseParser.ParseRecords<T>(json);
                    return records ?? [];
                }

            } else if (!string.IsNullOrEmpty(query)) {
                var json = await this._repository.FindByQueryAsync<T>(model, query);
                if (string.IsNullOrEmpty(json)) { return []; }

                var records = KintoneResponseParser.ParseRecords<T>(json);
                return records ?? [];

            } else {
                // 全件取得
                var json = await this._repository.FindAllAsync<T>(model, fieldCodes);
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
    public async Task<KintoneWriteResult<T>> UpdateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        try {
            this._logger?.LogInformation("UpdateAsync() - Start");

            var result = new KintoneWriteResult<T>();
            var chunks = records.Chunk(KintoneLimit).Select(c => c.ToList()).ToList();
            var semaphore = new SemaphoreSlim(this._execOptions.MaxConcurrency);

            var tasks = chunks.Select(async chunk => {
                await semaphore.WaitAsync();
                try {
                    var partial = await UpdateChunkAsync(chunk, enableSingleRetryOnError);
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
    private async Task<KintoneWriteResult<T>> UpdateChunkAsync<T>(IList<T> chunk, bool enableSingleRetryOnError) where T : KintoneModelBase<T>, new() {
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
    public async Task<KintoneDeleteResult> DeleteAsync<T>(IList<T> models, bool validateExistence = true) where T : KintoneModelBase<T>, new() {
        try {
            this._logger?.LogInformation("DeleteAsync() - Start");

            if (models.Count == 0) { return new KintoneDeleteResult(); }

            foreach (var model in models) {
                await model.RunBeforeDeleteHookAsync();
            }

            var target = validateExistence ? await this.PrepareValidatedTargets(models) : models;

            var result = new KintoneDeleteResult();
            if (validateExistence) {
                var failures = this.CollectNotFoundFailures(models, target);
                result.FailedIDs.AddRange(failures);
            }

            var chunks = target.Chunk(KintoneDeleteLimit).Select(c => c.ToList());
            var semaphore = new SemaphoreSlim(_execOptions.MaxConcurrency);

            var tasks = chunks.Select(async chunk => {
                await semaphore.WaitAsync();
                try {
                    var partial = await this.DeleteChunkAsync(chunk);
                    lock (result) {
                        result.DeletedIDs.AddRange(partial.DeletedIDs);
                        result.FailedIDs.AddRange(partial.FailedIDs);
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
    private async Task<KintoneDeleteResult> DeleteChunkAsync<T>(IList<T> chunk) where T : KintoneModelBase<T>, new() {
        var result = new KintoneDeleteResult();
        var idList = chunk.Select(m => m.RecordID).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).ToList();

        try {
            var validModels = chunk.Where(x => !string.IsNullOrWhiteSpace(x.RecordID)).ToList();
            await this._repository.DeleteRecordsAsync(validModels);
            result.DeletedIDs.AddRange(idList);

            foreach (var model in chunk.Where(m => idList.Contains(m.RecordID!))) {
                await model.RunAfterDeleteHookAsync();
            }

        } catch (KintoneException ex) {
            foreach (var id in idList) {
                result.FailedIDs.Add(new KintoneDeleteFailure {
                    ID = id,
                    ErrorMessage = ex.Message,
                    Reason = KintoneDeleteFailureReason.DeleteError
                });
            }
        }

        return result;
    }
    private async Task<IList<T>> PrepareValidatedTargets<T>(IList<T> models) where T : KintoneModelBase<T>, new() {
        var ids = models.Select(x => x.RecordID).ToList();
        return (await FindAsync<T>(ids, fieldCodes: ["RecordID"])).ToList();
    }
    private List<KintoneDeleteFailure> CollectNotFoundFailures<T>(IList<T> original, IList<T> found) where T : KintoneModelBase<T>, new() {
        var foundIds = found.Select(x => x.RecordID).ToHashSet();
        return original
            .Where(x => !foundIds.Contains(x.RecordID))
            .Select(x => new KintoneDeleteFailure {
                ID = x.RecordID,
                ErrorMessage = "Record is not found.",
                Reason = KintoneDeleteFailureReason.RecordNotFound
            }).ToList();
    }
    public async Task<KintoneWriteResult<T>> SaveAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        try {
            this._logger?.LogInformation("SaveAsync() - Start");

            var result = new KintoneWriteResult<T>();

            var (createTargets, updateTargets) = SplitRecords(records);

            if (createTargets.Count > 0) {
                var createResult = await CreateAsync(createTargets, enableSingleRetryOnError);
                result.Succeeded.AddRange(createResult.Succeeded);
                result.Failed.AddRange(createResult.Failed);
            }

            if (updateTargets.Count > 0) {
                var updateResult = await UpdateAsync(updateTargets, enableSingleRetryOnError);
                result.Succeeded.AddRange(updateResult.Succeeded);
                result.Failed.AddRange(updateResult.Failed);
            }

            return result;

        } finally {
            this._logger?.LogInformation("SaveAsync() - Finish");
        }
    }
    public async Task<KintoneWriteResult<T>> SaveWithRetryAsync<T>(IList<T> records, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) where T : KintoneModelBase<T>, new() {
        try {
            this._logger?.LogInformation("SaveWithRetryAsync() - Start");

            var result = new KintoneWriteResult<T>();

            var createTargets = new List<T>();
            var updateTargets = new List<T>();

            foreach (var record in records) {
                if (record.HasUpdateKeyOrID()) {
                    updateTargets.Add(record);
                } else {
                    createTargets.Add(record);
                }
            }

            // create 処理
            if (createTargets.Count > 0) {
                var createResult = await CreateAsync(createTargets, enableSingleRetryOnError);

                result.Succeeded.AddRange(createResult.Succeeded);
                result.Failed.AddRange(createResult.Failed);

                // 🔁 create に失敗したレコードを update として再試行
                if (enableCreateToUpdateRetry) {
                    var retryCandidates = createResult.Failed
                        .Where(f => f.Record.HasUpdateKeyOrID()) // update できる条件を満たす
                        .Select(f => f.Record)
                        .ToList();

                    if (retryCandidates.Count != 0) {
                        var updateResult = await UpdateAsync(retryCandidates, enableSingleRetryOnError);
                        result.Succeeded.AddRange(updateResult.Succeeded);
                        result.Failed.RemoveAll(f => retryCandidates.Contains(f.Record)); // 一度失敗したが成功に変わったものを除外
                        result.Failed.AddRange(updateResult.Failed); // 再試行の失敗分を追加
                    }
                }
            }

            // update 処理
            if (updateTargets.Count > 0) {
                var updateResult = await UpdateAsync(updateTargets, enableSingleRetryOnError);
                result.Succeeded.AddRange(updateResult.Succeeded);
                result.Failed.AddRange(updateResult.Failed);
            }

            return result;

        } finally {
            this._logger?.LogInformation("SaveWithRetryAsync() - Finish");
        }
    }
    private static IList<T> ParseUpdatedRecords<T>(IList<T> records, string responseJson) where T : KintoneModelBase<T>, new() {
        var indexResponse = KintoneRecordIndexesResponse.Parse(responseJson);
        var indexes = indexResponse.ToIndexes();

        // var recordList = records.ToList();
        var result = new List<T>();

        for (int i = 0; i < Math.Min(records.Count, indexes.IDs.Count); i++) {
            var model = records[i];
            model.RecordID = indexes.IDs[i] ?? string.Empty;
            model.Revision = int.TryParse(indexes.Revisions[i], out var revision) ? revision : -1;
            result.Add(model);
        }

        return result;
    }
    private static (List<T> createTargets, List<T> updateTargets) SplitRecords<T>(IList<T> records) where T : KintoneModelBase<T>, new() {
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
}
