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

/// <summary>
/// KintoneモデルのCRUD操作を提供するサービスクラス。
/// </summary>
/// <remarks>このクラスは、Kintoneアプリのレコードの作成、更新、削除、検索を行います。</remarks>
/// <param name="repository">Kintoneリポジトリ</param>
/// <param name="executionOptions">実行オプション</param>
/// <param name="jsonOptions">JSONシリアライゼーションオプション</param>
/// <param name="logger">ロガー</param>
public class KintoneModelCrudService( IKintoneRepository repository, IOptions<KintoneExecutionOptions>? executionOptions, JsonSerializerOptions? jsonOptions = null, ILogger<KintoneModelCrudService>? logger = null) : IKintoneModelCrudService {
    private readonly IKintoneRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<KintoneModelCrudService>? _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions ?? DefaultJsonOptions.Default;
    private readonly KintoneExecutionOptions _execOptions = executionOptions?.Value ?? new KintoneExecutionOptions();

    /// <summary>
    /// Kintoneモデルのレコードを作成します。
    /// </summary>
    /// <remarks>
    /// <para>このメソッドは、指定されたモデルのレコードをKintoneアプリに作成します。</para>
    /// <para>このメソッドは、バルク挿入を行い、失敗した場合は単件での再試行を行います。</para>
    /// <para>このメソッドは、KintoneのAPIを使用してレコードを作成します。</para>
    /// <para>バルク挿入の際に、Kintoneの制限により一度に挿入できるレコード数が制限されているため、複数のチャンクに分割して処理します。</para>
    /// <para>単件リトライを有効にすると、バルク挿入で失敗したレコードを個別に再試行します。</para>
    /// </remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="records">作成するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">単件リトライを有効にするかどうか</param>
    /// <returns>作成結果のリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneWriteResult<T>> CreateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
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
    /// <remarks>このメソッドは、バルク挿入を行い、失敗した場合は単件での再試行を行います。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="chunk">作成するレコードのチャンク</param>
    /// <param name="enableSingleRetryOnError">単件リトライを有効にするかどうか</param>
    /// <returns>作成結果のリスト</returns>
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

    /// <summary>
    /// Kintoneモデルのレコードを検索します。
    /// </summary>
    /// <remarks>このメソッドは、指定された条件に基づいてKintoneアプリのレコードを検索します。</remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="ids">検索するレコードのIDリスト</param>
    /// <param name="query">検索クエリ</param>
    /// <param name="fieldCodes">取得するフィールドコードのリスト</param>
    /// <returns>検索結果のレコードリスト</returns>
    /// <exception cref="KintoneException"></exception>
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

    /// <summary>
    /// Kintoneモデルのレコードを更新します。
    /// </summary>
    /// <remarks>このメソッドは、指定されたモデルのレコードをKintoneアプリに更新します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="records">更新するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">単件リトライを有効にするかどうか</param>
    /// <returns>更新結果のリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneWriteResult<T>> UpdateAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
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
    /// <remarks>このメソッドは、バルク更新を行い、失敗した場合は単件での再試行を行います。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="chunk">更新するレコードのチャンク</param>
    /// <param name="enableSingleRetryOnError">単件リトライを有効にするかどうか</param>
    /// <returns>更新結果のリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
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

    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <remarks>このメソッドは、指定されたモデルのレコードをKintoneアプリから削除します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="ids">削除するレコードのIDリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果のリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneDeleteResult> DeleteAsync<T>(IList<string> ids, bool validateExistence = true) where T : KintoneModelBase<T>, new() {
        var models = ids.Select(id => new T { RecordID = id }).ToList();
        return await this.DeleteAsync(models, validateExistence);
    }

    /// <summary>
    /// Kintoneモデルのレコードを削除します。
    /// </summary>
    /// <remarks>このメソッドは、指定されたモデルのレコードをKintoneアプリから削除します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="models">削除するレコードのモデルリスト</param>
    /// <param name="validateExistence">削除前にレコードの存在を検証するかどうか</param>
    /// <returns>削除結果のリスト</returns> 
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
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
    /// <remarks>このメソッドは、バルク削除を行い、失敗した場合は個別に再試行を行います。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="chunk">削除するレコードのチャンク</param>
    /// <returns>削除結果のリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
    private async Task<KintoneDeleteResult> DeleteChunkAsync<T>(IList<T> chunk) where T : KintoneModelBase<T>, new() {
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
    /// Kintoneモデルのレコードを削除する前に、存在を検証します。
    /// </summary> 
    /// <remarks>このメソッドは、削除対象のレコードがKintoneアプリに存在するかどうかを確認します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="models">検証するレコードのモデルリスト</param>
    /// <returns>存在が確認されたレコードのリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
    private async Task<IList<T>> PrepareValidatedTargets<T>(IList<T> models) where T : KintoneModelBase<T>, new() {
        var ids = models.Select(x => x.RecordID).ToList();
        return (await this.FindAsync<T>(ids, fieldCodes: ["RecordID"])).ToList();
    }

    /// <summary>
    /// 指定されたレコードのうち、存在しないものを収集します。
    /// </summary>
    /// <remarks>このメソッドは、削除対象のレコードのうち、Kintoneアプリに存在しないものを特定します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="original">元のレコードリスト</param>
    /// <param name="found">Kintoneアプリで見つかったレコードリスト</param>
    /// <returns>存在しないレコードの削除失敗リスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
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

    /// <summary>
    /// Kintoneモデルのレコードを保存します。
    /// </summary>
    /// <remarks>このメソッドは、指定されたモデルのレコードをKintoneアプリに保存します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="records">保存するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">単件リトライを有効にするかどうか</param>
    /// <returns>保存結果のリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
    public async Task<KintoneWriteResult<T>> SaveAsync<T>(IList<T> records, bool enableSingleRetryOnError = false) where T : KintoneModelBase<T>, new() {
        try {
            this._logger?.LogInformation("SaveAsync() - Start");

            var result = new KintoneWriteResult<T>();

            var (createTargets, updateTargets) = SplitRecords(records);

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
    /// Kintoneモデルのレコードを保存し、必要に応じて再試行を行います。
    /// </summary>
    /// <remarks>
    /// このメソッドは、指定されたモデルのレコードをKintoneアプリに保存します。
    /// <para>作成と更新を分けて処理し、作成に失敗したレコードを更新として再試行します。</para>
    /// <para>単件リトライを有効にすると、バルク挿入で失敗したレコードを個別に再試行します。</para>
    /// </remarks>
    /// ///<typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="records">保存するレコードのリスト</param>
    /// <param name="enableSingleRetryOnError">単件リトライを有効にするかどうか</param>
    /// <param name="enableCreateToUpdateRetry">作成に失敗したレコードを更新として再試行するかどうか</param>
    /// <returns>保存結果のリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
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
    /// 指定されたレコードの更新結果を解析します。
    /// </summary>
    /// <remarks>このメソッドは、KintoneのAPIレスポンスから更新されたレコードのIDとリビジョンを抽出します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="records">更新されたレコードのリスト</param>
    /// <param name="responseJson">Kintone APIからのレスポンスJSON</param>
    /// <returns>更新されたレコードのリスト</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
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

    /// <summary>
    /// 指定されたレコードを作成と更新のターゲットに分割します。
    /// </summary>
    /// <remarks>このメソッドは、レコードのリストを作成と更新のターゲットに分割します。</remarks>
    /// <typeparam name="T">KintoneModelBaseを継承したモデルクラス</typeparam>
    /// <param name="records">分割するレコードのリスト</param>
    /// <returns>作成ターゲットと更新ターゲットのタプル</returns>
    /// <exception cref="KintoneException">Kintone APIのエラーが発生した場合にスローされます。</exception>
    /// <exception cref="JsonException">JSONのシリアライズまたはデシリアライズに失敗した場合にスローされます。</exception>
    /// <exception cref="Exception">その他の予期しないエラーが発生した場合にスローされます。</exception>
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
