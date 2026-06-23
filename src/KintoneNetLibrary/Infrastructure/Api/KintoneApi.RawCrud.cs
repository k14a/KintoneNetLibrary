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
public partial class KintoneApi : IKintoneApi {
    /// <summary>
    /// 複数レコードを一括登録します（Raw）
    /// </summary>
    /// <param name="json">登録するレコードのJSON文字列</param>
    /// <returns>登録結果のJSON文字列</returns>
    public async Task<string> RawCreateAsync(string json) {
        return await this.CreateAsync(json);
    }

    /// <summary>
    /// 複数レコードを一括更新します（Raw）
    /// </summary>
    /// <param name="json">更新するレコードのJSON文字列</param>
    /// <returns>更新結果のJSON文字列</returns>
    /// <exception cref="ArgumentException">更新対象JSONが空の場合にスローされます</exception>
    /// <exception cref="KintoneException">Kintone API からのエラーが発生した場合にスローされます</exception>
    public async Task<string> RawUpdateAsync(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            throw new ArgumentException("更新対象JSONが空です", nameof(json));
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await this._httpClient.PutAsync(this.BuildRequestUri(KintoneApiEndpoints.UpdateRecords), content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (this._logger != null) { _logTraceException(this._logger, $"Received response: {responseJson}", null); }

        if (!response.IsSuccessStatusCode) {
            throw new KintoneException(KintoneErrorConverter.Parse(responseJson));
        }

        return responseJson;
    }

    /// <summary>
    /// 複数レコードを一括削除します（Raw）
    /// </summary>
    /// <param name="json">削除するレコードのJSON文字列</param>
    /// <returns>削除結果のJSON文字列</returns>
    public async Task<string> RawDeleteAsync(string json) {
        return await this.DeleteAsync(json);
    }
}