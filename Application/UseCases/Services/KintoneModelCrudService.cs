using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Domain.Common;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Api.DTO;
using KintoneNetLibrary.Infrastructure.Helpers;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KintoneNetLibrary.Infrastructure.Factories;

namespace KintoneNetLibrary.Application.UseCases.Services;

public class KintoneModelCrudService {
    private readonly IKintoneRepository _repository;
    private readonly ILogger<KintoneModelCrudService>? _logger;
    private readonly IKintoneApiFactory _apiFactory;
    private readonly KintoneAccount _account;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly KintoneExecutionOptions _execOptions;

    public KintoneModelCrudService(
        IKintoneRepository repository,
        IKintoneApiFactory apiFactory,
        IOptions<KintoneAccount> accountOptions,
        ILogger<KintoneModelCrudService> logger,
        IOptions<KintoneExecutionOptions>? executionOptions,
        JsonSerializerOptions? jsonOptions = null
    ) {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._apiFactory = apiFactory ?? throw new ArgumentNullException(nameof(apiFactory));
        this._account = accountOptions.Value ?? throw new ArgumentNullException(nameof(accountOptions));
        this._logger = logger;
        this._jsonOptions = jsonOptions ?? DefaultJsonOptions.Default;
        this._execOptions = executionOptions?.Value ?? new KintoneExecutionOptions();
    }

    public async Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase, new() {
        _logger?.LogInformation("CreateAsync() - Start");

        var result = new KintoneWriteResult<T>();
        var chunks = records.Chunk(KintoneLimit).ToList();
        var semaphore = new SemaphoreSlim(_execOptions.MaxConcurrency);

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
        _logger?.LogInformation("CreateAsync() - Finish");
        return result;
    }

    private async Task<KintoneWriteResult<T>> CreateChunkAsync<T>(IList<T> chunk, bool enableSingleRetryOnError) where T : KintoneModelBase, new() {
        var result = new KintoneWriteResult<T>();

        try {
            // var json = KintoneRequestBuilder.BuildCreateJson(chunk);
            var responseJson = await _repository.CreateRecordsAsync(chunk);
            var parsed = KintoneResponseParser.ParseCreatedRecords(chunk, responseJson);
            result.Succeeded.AddRange(parsed);

        } catch (KintoneException ex) {
            _logger?.LogWarning("Bulk insert failed: {Summary}", ex.Message);

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
                    // var singleJson = KintoneRequestBuilder.BuildCreateJson([record]);
                    var singleRespJson = await _repository.CreateRecordsAsync([record]);
                    var parsed = KintoneResponseParser.ParseCreatedRecords([record], singleRespJson);
                    result.Succeeded.AddRange(parsed);
                } catch (KintoneException singleEx) {
                    _logger?.LogError("Single insert failed: {Summary} - Record: {Record}", singleEx.Message, record);
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

    public async Task<IEnumerable<T>> FindAsync<T>(IEnumerable<string>? ids = null, string? query = null) where T : KintoneModelBase, new() {
        try {
            T model = new();

            if (ids != null && ids.Any()) {
                var idList = ids.ToList();

                // IDが1件なら単一取得
                if (idList.Count == 1) {
                    var json = await _repository.FindByIDAsync<T>(model, idList[0]);
                    if (string.IsNullOrEmpty(json)) { return []; }

                    var record = JsonSerializer.Deserialize<KintoneResponseWrapper<T>>(json);
                    if (record?.Record == null) { return []; }

                    return [record.Record];

                } else {
                    var json = await _repository.FindByIDsAsync<T>(model, idList);
                    if (string.IsNullOrEmpty(json)) { return []; }

                    var records = JsonSerializer.Deserialize<KintoneResponseListWrapper<T>>(json);
                    return records?.Records ?? [];
                }
            } else if (!string.IsNullOrEmpty(query)) {
                var json = await _repository.FindByQueryAsync<T>(model, query);
                if (string.IsNullOrEmpty(json)) { return []; }

                var records = JsonSerializer.Deserialize<KintoneResponseListWrapper<T>>(json);
                return records?.Records ?? [];
            } else {
                // 全件取得
                var json = await _repository.FindAllAsync<T>(model);
                if (string.IsNullOrEmpty(json)) { return []; }

                var records = JsonSerializer.Deserialize<KintoneResponseListWrapper<T>>(json);
                return records?.Records ?? [];
            }
        } catch (JsonException ex) {
            this._logger?.LogError(ex, "JSON deserialization failed in FindAsync<{Model}>", typeof(T).Name);
            throw new KintoneException("Failed to parse Kintone JSON response.", ex);
        } catch (Exception ex) {
            this._logger?.LogError(ex, "Unexpected error occurred in FindAsync<{Model}>", typeof(T).Name);
            throw new KintoneException("An unexpected error occurred while retrieving Kintone records.", ex);
        }
    }
    public async Task<KintoneWriteResult<T>> UpdateAsync<T>(IEnumerable<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase, new() {
        _logger?.LogInformation("UpdateAsync() - Start");

        var result = new KintoneWriteResult<T>();
        var chunks = records.Chunk(KintoneLimit).Select(c => c.ToList()).ToList();
        var semaphore = new SemaphoreSlim(_execOptions.MaxConcurrency);

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

        _logger?.LogInformation("UpdateAsync() - Finish");
        return result;
    }
    private async Task<KintoneWriteResult<T>> UpdateChunkAsync<T>(IList<T> chunk, bool enableSingleRetryOnError) where T : KintoneModelBase, new() {
        var result = new KintoneWriteResult<T>();

        try {
            // var json = KintoneRequestBuilder.BuildUpdateJson(chunk);
            var responseJson = await _repository.UpdateRecordsAsync(chunk);
            var parsed = ParseUpdatedRecords(chunk, responseJson);
            result.Succeeded.AddRange(parsed);
        } catch (KintoneException ex) {
            _logger?.LogWarning("Bulk update failed: {Summary}", ex.Message);

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
                    // var singleJson = KintoneRequestBuilder.BuildUpdateJson([record]);
                    var singleRespJson = await _repository.UpdateRecordsAsync([record]);
                    var parsed = ParseUpdatedRecords([record], singleRespJson);
                    result.Succeeded.AddRange(parsed);
                } catch (KintoneException singleEx) {
                    _logger?.LogError("Single update failed: {Summary} - Record: {Record}", singleEx.Message, record);
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
    public async Task<KintoneDeleteResult> DeleteAsync<T>(IEnumerable<T> models) where T : KintoneModelBase {
        _logger?.LogInformation("DeleteAsync() - Start");

        var modelList = models.ToList();
        if (modelList.Count == 0) {
            return new KintoneDeleteResult();
        }

        foreach (var model in modelList) {
            await model.RunBeforeDeleteHookAsync();
        }

        var chunks = modelList.Chunk(KintoneDeleteLimit).Select(c => c.ToList()).ToList();
        var semaphore = new SemaphoreSlim(_execOptions.MaxConcurrency);
        var result = new KintoneDeleteResult();

        var tasks = chunks.Select(async chunk => {
            await semaphore.WaitAsync();
            try {
                var partial = await DeleteChunkAsync(chunk);
                lock (result) {
                    result.DeletedIDs.AddRange(partial.DeletedIDs);
                    result.FailedIDs.AddRange(partial.FailedIDs);
                }
            } finally {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _logger?.LogInformation("DeleteAsync() - Finish");
        return result;
    }
    private async Task<KintoneDeleteResult> DeleteChunkAsync<T>(IList<T> chunk) where T : KintoneModelBase {
        var result = new KintoneDeleteResult();
        var idList = chunk.Select(m => m.RecordID).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).ToList();

        try {
            // string json = KintoneRequestBuilder.BuildDeleteJson(chunk);
            await _repository.DeleteRecordsAsync(chunk);
            result.DeletedIDs.AddRange(idList);

            foreach (var model in chunk.Where(m => idList.Contains(m.RecordID!))) {
                await model.RunAfterDeleteHookAsync();
            }
        } catch (KintoneException ex) {
            foreach (var id in idList) {
                result.FailedIDs.Add(new KintoneDeleteFailure {
                    ID = id,
                    ErrorMessage = ex.Message
                });
            }
        }

        return result;
    }

    public async Task<KintoneWriteResult<T>> SaveAsync<T>(IEnumerable<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase, new() {
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
    }

    public async Task<KintoneWriteResult<T>> SaveWithRetryAsync<T>(IEnumerable<T> records, bool enableSingleRetryOnError = false, bool enableCreateToUpdateRetry = true) where T : KintoneModelBase, new() {
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
    }
    private IList<T> ParseUpdatedRecords<T>(IEnumerable<T> records, string responseJson) where T : KintoneModelBase, new() {
        var indexResponse = KintoneRecordIndexesResponse.Parse(responseJson);
        var indexes = indexResponse.ToIndexes();

        var recordList = records.ToList();
        var result = new List<T>();

        for (int i = 0; i < Math.Min(recordList.Count, indexes.IDs.Count); i++) {
            var model = recordList[i];
            model.RecordID = indexes.IDs[i] ?? string.Empty;
            model.Revision = int.TryParse(indexes.Revisions[i], out var revision) ? revision : -1;
            result.Add(model);
        }

        return result;
    }
    private static (List<T> createTargets, List<T> updateTargets) SplitRecords<T>(IEnumerable<T> records) where T : KintoneModelBase {
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
