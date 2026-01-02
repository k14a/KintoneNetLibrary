namespace KintoneNetLibrary.Domain.Interfaces;

// コメントは日本語で記述
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
