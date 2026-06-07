using KintoneNetLibrary.Application.UseCases;
using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintone レコード取得 API インターフェイス
/// </summary>
public interface IKintoneRecordReadApi {
    /// <summary>
    /// Idで単一レコードを取得
    /// </summary>
    Task<string?> FindByIdAsync<T>(string id) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// Idで単一レコードを取得（Raw）
    /// </summary>
    Task<string?> RawFindByIdAsync(string id);

    /// <summary>
    /// Idで単一レコードを取得（Raw・ストリーム）
    /// </summary>
    Task RawFindByIdAsStreamAsync(Stream output, string id);

    /// <summary>
    /// Idリストで複数レコードを取得
    /// </summary>
    Task<string?> FindByIdsAsync<T>(IList<string> ids, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// Idリストで複数レコードを取得（Raw）
    /// </summary>
    Task<string?> RawFindByIdsAsync(IList<string> ids, IList<string>? fieldCodes = null);

    /// <summary>
    /// Idリストで複数レコードを取得（Raw・ストリーム）
    /// </summary>
    Task RawFindByIdsAsStreamAsync(Stream output, IList<string> ids, IList<string>? fieldCodes = null);

    /// <summary>
    /// 全レコード取得（条件なし）
    /// </summary>
    Task<string?> FindAllAsync<T>(IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 全レコード取得（条件なし・Raw）
    /// </summary>
    Task<string?> RawFindAllAsync(IList<string>? fieldCodes = null);

    /// <summary>
    /// 全レコード取得（条件なし・Raw・ストリーム）
    /// </summary>
    Task RawFindAllAsStreamAsync(Stream output, IList<string>? fieldCodes = null);

    /// <summary>
    /// 指定フィールド＝値 で検索
    /// </summary>
    Task<string?> FindByFieldAsync<T>(string field, string value) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw）
    /// </summary>
    Task<string?> RawFindByFieldAsync(string field, string value);

    /// <summary>
    /// 指定フィールド＝値 で検索（Raw・ストリーム）
    /// </summary>
    Task RawFindByFieldAsStreamAsync(Stream output, string field, string value, IList<string>? fieldCodes = null);

    /// <summary>
    /// KintoneQueryで検索
    /// </summary>
    Task<string?> FindByKintoneQueryAsync<T>(KintoneQuery<T> query, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// クエリ文字列で検索
    /// </summary>
    Task<string?> FindByQueryAsync<T>(string queryStr, IList<string>? fieldCodes = null) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// クエリ文字列で検索（Raw）
    /// </summary>
    Task<string?> RawFindByQueryAsync(string queryStr, IList<string>? fieldCodes = null);

    /// <summary>
    /// クエリ文字列で検索（Raw・ストリーム）
    /// </summary>
    Task RawFindByQueryAsStreamAsync(Stream output, string queryStr, IList<string>? fieldCodes = null);
}
