using KintoneNetLibrary.Domain.Entities;

namespace KintoneNetLibrary.Application.Interfaces;

/// <summary>
/// Kintone レコード書き込み API インターフェイス
/// </summary>
public interface IKintoneRecordWriteApi {
    /// <summary>
    /// 複数レコードを一括登録します
    /// </summary>
    Task<string> CreateAsync(string json);

    /// <summary>
    /// 複数レコードを一括登録します（Raw）
    /// </summary>
    Task<string> RawCreateAsync(string json);

    /// <summary>
    /// 複数レコードを一括更新します
    /// </summary>
    Task<string> UpdateAsync<T>(string json) where T : KintoneModelBase<T>, new();

    /// <summary>
    /// 複数レコードを一括更新します（Raw）
    /// </summary>
    Task<string> RawUpdateAsync(string json);

    /// <summary>
    /// 複数レコードを一括削除します
    /// </summary>
    Task<string> DeleteAsync(string json);

    /// <summary>
    /// 複数レコードを一括削除します（Raw）
    /// </summary>
    Task<string> RawDeleteAsync(string json);
}
