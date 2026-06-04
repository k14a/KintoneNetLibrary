namespace KintoneNetLibrary.Domain.Entities;

/// <summary>
/// Kintone の項目名と C# 側のプロパティ名の変換ルール。
/// 変換方向（Read, Send, Both）を指定して使用します。
/// </summary>
public class NameConverter {
    /// <summary>
    /// kintone 上の項目名
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 対応する C# のプロパティ名
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 変換方向（読み取り / 書き出し / 両方）
    /// </summary>
    public Direction ConvertDirection { get; set; } = Direction.Both;

    /// <summary>
    /// 変換方向
    /// </summary>
    public enum Direction {
        /// <summary>
        /// 両方
        /// </summary>
        Both,
        /// <summary>
        /// 読み取り専用
        /// </summary>
        Read,
        /// <summary>
        /// 書き出し専用
        /// </summary>
        Send
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public NameConverter() { }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="itemName">Kintone上の項目名</param>
    /// <param name="propertyName">対応するC#のプロパティ名</param>
    /// <param name="direction">変換方向（読み取り / 書き出し / 両方）</param>
    public NameConverter(string itemName, string propertyName, Direction direction = Direction.Both) {
        this.ItemName = itemName;
        this.PropertyName = propertyName;
        this.ConvertDirection = direction;
    }

    /// <summary>
    /// ファクトリーメソッド
    /// </summary>
    /// <param name="itemName">Kintone上の項目名</param>
    /// <param name="propertyName">対応するC#のプロパティ名</param>
    /// <param name="direction">変換方向（読み取り / 書き出し / 両方）</param>
    /// <returns>新しいNameConverterインスタンス</returns>
    public static NameConverter Create(string itemName, string propertyName, Direction direction = Direction.Both) {
        return new NameConverter(itemName, propertyName, direction);
    }
}
