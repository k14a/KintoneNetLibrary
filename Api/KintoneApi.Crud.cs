using System.Net.Http.Json;
using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Model;
using KintoneNetLibrary.Types;

namespace KintoneNetLibrary.Api;

public partial class KintoneApi
{
    //private static readonly JsonSerializerOptions _jsonOpt = KintoneJson.Options;

    /*==========================================================
      Create – 複数レコード一括登録
      ==========================================================*/
    public async Task<KintoneIndexes> CreateAsync<T>(IList<T> objs) where T : KintoneModelBase {
        var body = new {
            app = objs[0].AppID,
            records = objs
        };

        var response = await _httpClient.PostAsJsonAsync("records.json", body, _jsonOptions);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) { throw new KintoneException(KintoneErrorConverter.Parse(json)); }

        return JsonSerializer.Deserialize<KintoneIndexes>(json, _jsonOptions) ?? new KintoneIndexes();
    }

    /*==========================================================
      Update – 複数レコード一括更新
      ==========================================================*/
    public async Task<KintoneIndexes> UpdateAsync<T>(IList<T> objs) where T : KintoneModelBase {
        var body = new {
            app = objs[0].AppID,
            records = objs
        };

        var response = await _httpClient.PutAsJsonAsync("records.json", body, _jsonOptions);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) { throw new KintoneException(KintoneErrorConverter.Parse(json)); }

        return JsonSerializer.Deserialize<KintoneIndexes>(json, _jsonOptions) ?? new KintoneIndexes();
    }

    /*==========================================================
      Delete – ID リストで一括削除
      ==========================================================*/
    public async Task<bool> DeleteAsync<T>(IList<string> ids) where T : KintoneModelBase, new() {
        var sample = new T();
        var body = new { app = sample.AppID, ids };

        var request = new HttpRequestMessage(HttpMethod.Delete, "records.json") {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) { throw new KintoneException(KintoneErrorConverter.Parse(json)); }

        return true;
    }
    public async Task<bool> DeleteAsync(int appId, IList<string> ids) {
        var body = new { app = appId, ids };
        var request = new HttpRequestMessage(HttpMethod.Delete, "records.json") {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };

        var response = await this._httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(json));
        }

        return true;
    }

    /*==========================================================
      Save – キー項目を用いた Upsert
      ==========================================================*/
    public async Task<KintoneIndexes> SaveAsync<T>(IList<T> objs) where T : KintoneModelBase {
        // kintone の Upsert API がないため、内部で Create or Update の振り分け例
        var createTargets = objs.Where(o => string.IsNullOrEmpty(o.RecordID)).ToList();
        var updateTargets = objs.Where(o => !string.IsNullOrEmpty(o.RecordID)).ToList();

        var indexes = new KintoneIndexes();

        if (createTargets.Count > 0) {
            var created = await CreateAsync(createTargets);
            indexes.IDs.AddRange(created.IDs);
            indexes.Revisions.AddRange(created.Revisions);
        }

        if (updateTargets.Count > 0) {
            var updated = await UpdateAsync(updateTargets);
            indexes.IDs.AddRange(updated.IDs);
            indexes.Revisions.AddRange(updated.Revisions);
        }

        return indexes;
    }
}
