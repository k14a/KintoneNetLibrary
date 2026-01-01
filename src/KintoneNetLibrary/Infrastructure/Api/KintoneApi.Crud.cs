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

// コメントは日本語で記述
/// <summary>
/// Kintone API の CRUD 操作を提供する部分クラス
/// </summary>
public partial class KintoneApi {
    /// <summary>
    /// 複数レコードを一括登録します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<string> CreateAsync(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("JSONデータが空です", nameof(json));
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await this._httpClient.PostAsync(KintoneApiEndpoints.AddRecords, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        this._logger?.LogDebug("Received response from Kintone: {Response}", responseJson);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /// <summary>
    /// 複数レコードを一括更新します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<string> UpdateAsync<T>(string json) where T : KintoneModelBase<T>, new() {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("更新対象JSONが空です", nameof(json));
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await this._httpClient.PutAsync(KintoneApiEndpoints.UpdateRecords, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        this._logger?.LogDebug("Received response: {Response}", responseJson);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /// <summary>
    /// 複数レコードを一括削除します
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="KintoneException"></exception>
    public async Task<string> DeleteAsync(string json) {
        var request = new HttpRequestMessage(HttpMethod.Delete, KintoneApiEndpoints.DeleteRecords) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await this._httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        this._logger?.LogDebug("Delete response: {Response}", responseJson);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

}
