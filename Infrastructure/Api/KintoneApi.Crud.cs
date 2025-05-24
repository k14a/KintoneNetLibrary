using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {
    /*==========================================================
      Create – 複数レコード一括登録
      ==========================================================*/
    public async Task<KintoneIndexes> CreateAsync<T>(IEnumerable<T> objs) where T : KintoneModelBase {
        if (objs == null || !objs.Any()) {
            throw new ArgumentException("登録対象が空です", nameof(objs));
        }

        var records = objs.Select(x => x.ToKintoneRecord()).ToList();
        var body = new {
            app = objs.First().AppID,
            records,
        };

        var response = await _httpClient.PostAsJsonAsync("records.json", body, _jsonOptions);
        var json = await response.Content.ReadAsStringAsync();
        _logger?.LogDebug("Received response: {Response}", json);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var tmp = JsonSerializer.Deserialize<KintoneRecordIndexesResponse>(json, _jsonOptions)
                  ?? new KintoneRecordIndexesResponse();

        return new KintoneIndexes {
            IDs = tmp.Records.Select(x => x.ID).ToList(),
            Revisions = tmp.Records.Select(x => x.RevisionString).ToList(),
        };
    }

    /*==========================================================
      Update – 複数レコード一括更新
      ==========================================================*/
    public async Task<KintoneIndexes> UpdateAsync<T>(IEnumerable<T> objs) where T : KintoneModelBase {
        if (objs == null || !objs.Any()) {
            throw new ArgumentException("更新対象が空です", nameof(objs));
        }

        var records = new List<object>();

        foreach (var obj in objs) {
            var record = new Dictionary<string, object>();
            string? updateKeyField = null;
            object? updateKeyValue = null;

            foreach (var prop in typeof(T).GetProperties()) {
                var attr = prop.GetCustomAttribute<KintoneItemAttribute>();
                if (attr == null || !attr.IsUpload) {
                    continue;
                }

                var fieldCode = string.IsNullOrEmpty(attr.FieldCode) ? prop.Name : attr.FieldCode;
                var value = prop.GetValue(obj);

                if (attr.IsKey) {
                    updateKeyField = fieldCode;
                    updateKeyValue = value;
                    continue;
                }

                record[fieldCode] = new { value };
            }

            object recordObject;

            if (updateKeyField != null) {
                recordObject = new {
                    updateKey = new {
                        field = updateKeyField,
                        value = updateKeyValue
                    },
                    record
                };
            } else {
                recordObject = new {
                    id = obj.RecordID,
                    record
                };
            }

            records.Add(recordObject);
        }

        var body = new {
            app = objs.First().AppID,
            records
        };

        var response = await _httpClient.PutAsJsonAsync("records.json", body, _jsonOptions);
        var json = await response.Content.ReadAsStringAsync();
        _logger?.LogDebug("Received response: {Response}", json);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        var tmp = JsonSerializer.Deserialize<KintoneRecordIndexesResponse>(json, _jsonOptions)
                  ?? new KintoneRecordIndexesResponse();

        return new KintoneIndexes {
            IDs = tmp.Records.Select(x => x.ID).ToList(),
            Revisions = tmp.Records.Select(x => x.RevisionString).ToList(),
        };
    }

    /*==========================================================
      Delete – ID リストで一括削除
      ==========================================================*/
    public async Task<KintoneDeleteResult> DeleteAsync<T>(IList<string> ids) where T : KintoneModelBase, new() {
        var t = new T();
        return await this.DeleteAsync(t.AppID, ids);
    }

    public async Task<KintoneDeleteResult> DeleteAsync(int appId, IList<string> ids) {
        var result = new KintoneDeleteResult();

        foreach (var chunk in ids.Chunk(KintoneDeleteLimit)) {
            var body = new { app = appId, ids = chunk };
            var request = new HttpRequestMessage(HttpMethod.Delete, "records.json") {
                Content = JsonContent.Create(body, options: _jsonOptions)
            };

            var response = await this._httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode) {
                result.DeletedIDs.AddRange(chunk);
            } else {
                var error = KintoneErrorConverter.Parse(json);
                foreach (var id in chunk) {
                    result.FailedIDs.Add(new KintoneDeleteFailure {
                        ID = id,
                        ErrorMessage = error.Message
                    });
                }
            }
        }

        return result;
    }
}
