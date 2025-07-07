using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using KintoneNetLibrary.Extensions;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using static KintoneNetLibrary.Domain.Common.KintoneConstants;
using Microsoft.Extensions.Logging;
using KintoneNetLibrary.Infrastructure.Internal;

namespace KintoneNetLibrary.Infrastructure.Api;

public partial class KintoneApi {
    /*==========================================================
      Create – 複数レコード一括登録
      ==========================================================*/
    public async Task<string> CreateAsync(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("JSONデータが空です", nameof(json));
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(KintoneApiEndpoints.AddRecords, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("Received response from Kintone: {Response}", responseJson);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /*==========================================================
      Update – 複数レコード一括更新
      ==========================================================*/
    public async Task<string> UpdateAsync<T>(string json) where T : KintoneModelBase {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("更新対象JSONが空です", nameof(json));
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(KintoneApiEndpoints.UpdateRecords, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("Received response: {Response}", responseJson);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /*==========================================================
      Delete – ID リストで一括削除
      ==========================================================*/
    public async Task<string> DeleteAsync(string json) {
        var request = new HttpRequestMessage(HttpMethod.Delete, KintoneApiEndpoints.DeleteRecords) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("Delete response: {Response}", responseJson);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

}
