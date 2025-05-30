using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Api.DTO;
using KintoneNetLibrary.Infrastructure.Helpers;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KintoneNetLibrary.Infrastructure.Factories;
using KintoneNetLibrary.Domain.Common;

namespace KintoneNetLibrary.Application.UseCases.Services;

public class KintoneModelCrudService {
    private readonly IKintoneRepository _repository;
    private readonly ILogger<KintoneModelCrudService>? _logger;
    private readonly IKintoneApiFactory _apiFactory;
    private readonly KintoneAccount _account;
    private static readonly JsonSerializerOptions _jsonOptions = KintoneJsonOptions.Default;

    public KintoneModelCrudService(IKintoneRepository repository, IKintoneApiFactory apiFactory, IOptions<KintoneAccount> accountOptions, ILogger<KintoneModelCrudService> logger) {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._apiFactory = apiFactory ?? throw new ArgumentNullException(nameof(apiFactory));
        this._account = accountOptions.Value ?? throw new ArgumentNullException(nameof(accountOptions));
        this._logger = logger;
    }

    public async Task<KintoneWriteResult<T>> CreateAsync<T>(IEnumerable<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase, new() {
        var result = new KintoneWriteResult<T>();

        foreach (var chunk in records.Chunk(KintoneLimit)) {
            try {
                var json = KintoneRequestBuilder.BuildCreateJson(chunk);
                var responseJson = await _repository.CreateRecordsAsync<T>(json);
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
                    continue;
                }

                // 単件ずつ再実行
                foreach (var record in chunk) {
                    try {
                        var singleJson = BuildCreateJson([record]);
                        var singleRespJson = await this._repository.CreateRecordsAsync<T>(singleJson);
                        var parsed = ParseCreatedRecords<T>([record], singleRespJson);
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
        }

        return result;
    }

    public async Task<IEnumerable<T>> FindAsync<T>(IEnumerable<string>? ids = null, string? query = null) where T : KintoneModelBase, new() {
        try {
            if (ids != null && ids.Any()) {
                var idList = ids.ToList();

                // IDが1件なら単一取得
                if (idList.Count == 1) {
                    var json = await _repository.FindByIDAsync<T>(idList[0]);
                    if (string.IsNullOrEmpty(json)) {
                        return [];
                    }

                    var record = JsonSerializer.Deserialize<KintoneResponseWrapper<T>>(json);
                    if (record?.Record == null) {
                        return [];
                    }

                    return [record.Record];
                } else {
                    var json = await _repository.FindByIDsAsync<T>(idList);
                    if (string.IsNullOrEmpty(json)) {
                        return [];
                    }

                    var records = JsonSerializer.Deserialize<KintoneResponseListWrapper<T>>(json);
                    return records?.Records ?? [];
                }
            } else if (!string.IsNullOrEmpty(query)) {
                var json = await _repository.FindByQueryAsync<T>(query);
                if (string.IsNullOrEmpty(json)) {
                    return [];
                }

                var records = JsonSerializer.Deserialize<KintoneResponseListWrapper<T>>(json);
                return records?.Records ?? [];
            } else {
                // 全件取得
                var json = await _repository.FindAllAsync<T>();
                if (string.IsNullOrEmpty(json)) {
                    return [];
                }

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
        var result = new KintoneWriteResult<T>();

        foreach (var chunk in records.Chunk(KintoneLimit)) {
            try {
                var json = KintoneRequestBuilder.BuildUpdateJson(chunk);
                var responseJson = await _repository.UpdateAsync<T>(json);
                var parsed = ParseUpdatedRecords(chunk, responseJson);

                foreach (var item in parsed) {
                    result.Succeeded.Add(item);
                }
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

                    continue;
                }

                // 単件リトライ
                foreach (var record in chunk) {
                    try {
                        var singleJson = BuildUpdateJson([record]);
                        var singleRespJson = await _repository.UpdateAsync<T>(singleJson);
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
        }

        return result;
    }

    public async Task<KintoneDeleteResult> DeleteAsync<T>(IEnumerable<T> models) where T : KintoneModelBase {
        var modelList = models.ToList();

        if (modelList.Count == 0) {
            return new KintoneDeleteResult();
        }

        foreach (var model in modelList) {
            await model.RunBeforeDeleteHookAsync();
        }

        string json = KintoneRequestBuilder.BuildDeleteJson(modelList);
        List<string> idList = modelList.Select(m => m.RecordID).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

        try {
            await _repository.DeleteAsync<T>(json);
        } catch (KintoneException ex) {
            var result = new KintoneDeleteResult();
            foreach (var id in idList) {
                result.FailedIDs.Add(new KintoneDeleteFailure {
                    ID = id,
                    ErrorMessage = ex.Message
                });
            }
            return result;
        }

        var deleteResult = new KintoneDeleteResult {
            DeletedIDs = idList
        };

        foreach (var model in modelList.Where(m => idList.Contains(m.RecordID))) {
            await model.RunAfterDeleteHookAsync();
        }

        return deleteResult;
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

    [Obsolete()]
    private string BuildCreateJson<T>(IEnumerable<T> records) where T : KintoneModelBase {
        var list = records.ToList();
        var appID = list.First().AppID;

        var body = new {
            app = appID,
            records = list.Select(r => r.ToKintoneRecord())
        };

        return JsonSerializer.Serialize(body, _jsonOptions);
    }
    [Obsolete()]
    private string BuildUpdateJson<T>(IEnumerable<T> records) where T : KintoneModelBase, new() {
        var jsonObj = new Dictionary<string, object> {
            ["records"] = records.Select(r => r.ToKintoneUpdateRecord())
        };

        return JsonSerializer.Serialize(jsonObj);
    }
    [Obsolete()]
    private IList<T> ParseCreatedRecords<T>(IEnumerable<T> originalRecords, string responseJson)
        where T : KintoneModelBase, new() {
        var indexes = KintoneRecordIndexesResponse.Parse(responseJson).ToIndexes();

        var originals = originalRecords.ToList();
        if (indexes.IDs.Count != originals.Count) {
            throw new KintoneException("Mismatch between the number of request and response records.");
        }

        for (int i = 0; i < originals.Count; i++) {
            originals[i].ID = indexes.IDs[i] ?? string.Empty;
            if (int.TryParse(indexes.Revisions[i], out var rev)) {
                originals[i].Revision = rev;
            }
        }

        return originals;
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
