using System.Text;
using KintoneNetLibrary.Application.Interfaces;
using KintoneNetLibrary.Domain.Entities;
using KintoneNetLibrary.Infrastructure.Converters;
using KintoneNetLibrary.Infrastructure.Internal;
using Microsoft.Extensions.Logging;

namespace KintoneNetLibrary.Infrastructure.Api;

/// <summary>
/// レコード一括登録・更新・削除（Raw）
/// </summary>
public partial class KintoneApi : BaseKintoneApi, IKintoneApi {
    /// <summary>
    /// 複数レコードを一括登録します（Raw）
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public async Task<string> RawCreateAsync(string json) {
        return await this.CreateAsync(json);
    }

    /// <summary>
    /// 複数レコードを一括更新します（Raw）
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="KintoneException"></exception>
    public async Task<string> RawUpdateAsync(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("更新対象JSONが空です", nameof(json));
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await this._httpClient.PutAsync(KintoneApiEndpoints.UpdateRecords, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        this._logger?.LogTrace("Received response: {Response}", responseJson);

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /// <summary>
    /// 複数レコードを一括更新します（Raw）
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public async Task<string> RawDeleteAsync(string json) {
        return await this.DeleteAsync(json);
    }
}