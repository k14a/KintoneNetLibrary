using System.Text.Json;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Domain.Interfaces;
using KintoneNetLibrary.Infrastructure.Api.DTO;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Application.UseCases.Services;

public class KintoneModelCrudService {
    private readonly IKintoneRepository _repository;
    private readonly ILogger<KintoneModelCrudService>? _logger;

    public KintoneModelCrudService(IKintoneRepository repository) {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    public KintoneModelCrudService(IKintoneRepository repository, ILogger<KintoneModelCrudService> logger) {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<T>> CreateAsync<T>(IEnumerable<T> models) where T : KintoneModelBase {
        ArgumentNullException.ThrowIfNull(models);

        var modelList = models.ToList();
        foreach (var model in modelList) {
            await model.RunBeforeCreateHookAsync();
        }

        var indexes = await _repository.CreateAsync(modelList); // KintoneIndexes 型

        // ID/Revisionを KintoneModel にマッピング
        for (int i = 0; i < modelList.Count && i < indexes.IDs.Count; i++) {
            var model = modelList[i];
            model.RecordID = indexes.IDs[i] ?? string.Empty;
            model.Revision = int.TryParse(indexes.Revisions[i], out var rev) ? rev : -1;
        }

        foreach (var model in modelList) {
            await model.RunAfterCreateHookAsync();
        }

        return modelList;
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


    public async Task<IEnumerable<T>> UpdateAsync<T>(IEnumerable<T> models) where T : KintoneModelBase {
        var modelList = models.ToList();

        foreach (var model in modelList) {
            await model.RunBeforeUpdateHookAsync();
        }

        var result = await _repository.UpdateAsync(modelList);

        // ID と Revision をモデルに反映
        for (int i = 0; i < modelList.Count; i++) {
            var model = modelList[i];
            if (i < result.IDs.Count) {
                model.RecordID = result.IDs[i] ?? model.RecordID;
            }
            if (i < result.Revisions.Count && int.TryParse(result.Revisions[i], out int rev)) {
                model.Revision = rev;
            }
        }

        foreach (var model in modelList) {
            await model.RunAfterUpdateHookAsync();
        }

        return modelList;
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

    public async Task<KintoneIndexes> SaveAsync<T>(IEnumerable<T> models) where T : KintoneModelBase {
        var toCreate = new List<T>();
        var toUpdate = new List<T>();

        foreach (var model in models) {
            if (string.IsNullOrEmpty(model.RecordID)) {
                toCreate.Add(model);
            } else {
                toUpdate.Add(model);
            }
        }

        var totalIndexes = new KintoneIndexes();

        if (toCreate.Count > 0) {
            foreach (var model in toCreate) {
                await model.RunBeforeCreateHookAsync();
            }

            var createIndexes = await _repository.CreateAsync(toCreate);

            foreach (var model in toCreate) {
                await model.RunAfterCreateHookAsync();
            }

            // 結果を統合
            foreach (var id in createIndexes.IDs) {
                totalIndexes.IDs.Add(id);
            }
            foreach (var rev in createIndexes.Revisions) {
                totalIndexes.Revisions.Add(rev);
            }
        }

        if (toUpdate.Count > 0) {
            foreach (var model in toUpdate) {
                await model.RunBeforeUpdateHookAsync();
            }

            var updateIndexes = await _repository.UpdateAsync(toUpdate);

            foreach (var model in toUpdate) {
                await model.RunAfterUpdateHookAsync();
            }

            // 結果を統合
            foreach (var id in updateIndexes.IDs) {
                totalIndexes.IDs.Add(id);
            }
            foreach (var rev in updateIndexes.Revisions) {
                totalIndexes.Revisions.Add(rev);
            }
        }

        return totalIndexes;
    }

}
