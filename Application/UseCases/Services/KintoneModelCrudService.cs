using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api;
using KintoneNetLibrary.Infrastructure.Api.DTO;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KintoneNetLibrary.Application.UseCases.Services;

public class KintoneModelCrudService {
    private readonly IKintoneRepository _repository;
    private readonly ILogger<KintoneModelCrudService>? _logger;
    private readonly KintoneApi _api;

    public KintoneModelCrudService(IKintoneRepository repository) {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    public KintoneModelCrudService(IKintoneRepository repository, ILogger<KintoneModelCrudService> logger) {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var apiOptions = new KintoneApiOptions { Domain = repository.Domain, AppID = repository.AppCode, ApiToken = repository.ApiToken };
        this._api = new KintoneApi(apiOptions, this.Logger);
    }

    public async Task<KintoneWriteResult<T>> CreateAsync<T>(IEnumerable<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase, new() {

        var result = new KintoneWriteResult<T>();

        foreach (var chunk in records.Chunk(KintoneLimit)) {
            try {
                var json = BuildCreateJson(chunk);
                var response = await this._api.PostAsync(json);
                var parsed = ParseCreatedRecords<T>(chunk, response);
                foreach (var item in parsed) {
                    result.Succeeded.Add(item);
                }
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

                    continue;
                }

                // 単件リトライ
                foreach (var record in chunk) {
                    try {
                        var singleJson = BuildCreateJson(new List<T> { record });
                        var singleResp = await this._api.PostAsync(singleJson);
                        var parsed = ParseCreatedRecords<T>([record], singleResp);
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
                var json = BuildUpdateJson(chunk);
                var response = await this._api.PutAsync(json);
                var parsed = ParseUpdatedRecords(chunk, response);
                foreach (var item in parsed) {
                    result.Succeeded.Add(item);
                }
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

                    continue;
                }

                // 単件リトライ
                foreach (var record in chunk) {
                    try {
                        var singleJson = BuildUpdateJson([record]);
                        var singleResp = await this._api.PutAsync(singleJson);
                        var parsed = ParseUpdatedRecords([record], singleResp);
                        result.Succeeded.AddRange(parsed);
                    } catch (KintoneException singleEx) {
                        logger?.LogError("Single update failed: {Summary} - Record: {Record}", singleEx.Message, record);
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
        foreach (var model in modelList) {
            await model.RunBeforeDeleteHookAsync();
        }

        var deleteResult = await _repository.DeleteAsync(modelList);

        // var deleteResult = KintoneDeleteResult.Parse(result, modelList.Select(m => m.RecordID));

        foreach (var model in modelList.Where(m => deleteResult.DeletedIDs.Contains(m.RecordID))) {
            await model.RunAfterDeleteHookAsync();
        }

        return deleteResult;
    }

    public async Task<KintoneWriteResult<T>> SaveAsync<T>(IEnumerable<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase, new() {
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

    private string BuildCreateJson<T>(IEnumerable<T> records) where T : KintoneModelBase, new() {
        var jsonObj = new Dictionary<string, object> {
            ["records"] = records.Select(r => r.ToKintoneRecord())
        };

        return JsonSerializer.Serialize(jsonObj);
    }
    private string BuildUpdateJson<T>(IEnumerable<T> records) where T : KintoneModelBase, new() {
        var jsonObj = new Dictionary<string, object> {
            ["records"] = records.Select(r => r.ToKintoneUpdateRecord())
        };

        return JsonSerializer.Serialize(jsonObj);
    }
    public virtual Dictionary<string, object> ToKintoneRecord() {
        var dict = new Dictionary<string, object>();

        foreach (var prop in this.GetType().GetProperties()) {
            var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
            if (attr == null) {
                continue;
            }

            var fieldCode = attr.FieldCode;
            var value = prop.GetValue(this);

            dict[fieldCode] = new { value };
        }

        return dict;
    }
}
