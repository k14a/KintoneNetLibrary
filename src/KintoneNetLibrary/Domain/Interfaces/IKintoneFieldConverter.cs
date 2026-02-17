namespace KintoneNetLibrary.Domain.Interfaces;

/// <summary>
/// Kintoneフィールドコンバーターのインターフェース
/// </summary>
public interface IKintoneFieldConverter {
    /// <summary>
    /// KintoneフィールドをJSON形式に変換します。
    /// </summary>
    /// <returns>JSON形式のフィールドデータ</returns>
    object? ToJson();
}
