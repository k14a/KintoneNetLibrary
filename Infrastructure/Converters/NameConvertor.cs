namespace KintoneNetLibrary.Infrastructure.Converters;

/// <summary>
/// Kintone の項目名と C# 側のプロパティ名の変換ルール。
/// 変換方向（Read, Send, Both）を指定して使用します。
/// </summary>
public class NameConvertor
{
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
    public enum Direction
    {
        Both,
        Read,
        Send
    }

    public NameConvertor() { }

    public NameConvertor(string itemName, string propertyName, Direction direction = Direction.Both)
    {
        this.ItemName = itemName;
        this.PropertyName = propertyName;
        this.ConvertDirection = direction;
    }

    public static NameConvertor Create(string itemName, string propertyName, Direction direction = Direction.Both)
    {
        return new NameConvertor(itemName, propertyName, direction);
    }
}
