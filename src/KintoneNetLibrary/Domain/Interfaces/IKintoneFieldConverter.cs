namespace KintoneNetLibrary.Domain.Interfaces;

// コメントは日本語で記述
/// <summary>
/// Kintoneフィールドコンバーターのインターフェース
/// </summary>
public interface IKintoneFieldConverter {
    object? ToJson();
}
