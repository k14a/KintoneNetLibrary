namespace KintoneNetLibrary.Types;

public class KintoneSubTableItem
{
    public string ID { get; set; } = string.Empty;

    public Dictionary<string, KintoneField> Value { get; set; } = new();
}
