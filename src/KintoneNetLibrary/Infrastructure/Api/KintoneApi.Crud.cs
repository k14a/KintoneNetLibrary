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
using KintoneNetLibrary.Application.Interfaces;

namespace KintoneNetLibrary.Infrastructure.Api;

/// <summary>
/// Kintone API の CRUD 操作を提供する部分クラス
/// </summary>
public partial class KintoneApi : IKintoneApi {
    /// <summary>
    /// 複数レコードを一括登録します
    /// </summary>
    /// <param name="json">登録対象のJSONデータ</param>
    /// <returns>登録結果のJSONデータ</returns>
    /// <exception cref="ArgumentException">JSONデータが空の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone APIからのエラーが発生した場合にスローされます</exception>
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
    /// <typeparam name="T">Kintoneのレコードデータの型</typeparam>
    /// <param name="json">更新対象のJSONデータ</param>
    /// <returns>更新結果のJSONデータ</returns>
    /// <exception cref="ArgumentException">JSONデータが空の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone APIからのエラーが発生した場合にスローされます</exception>
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
    /// <param name="json">削除対象のJSONデータ</param>
    /// <returns>削除結果のJSONデータ</returns>
    /// <exception cref="ArgumentException">JSONデータが空の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone APIからのエラーが発生した場合にスローされます</exception>
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
