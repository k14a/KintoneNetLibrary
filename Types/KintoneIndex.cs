namespace KintoneNetLibrary.Types;

/// <summary>
/// 単一レコードの ID / Revision を表す DTO.
/// </summary>
public class KintoneIndex
{
    public string ID { get; set; } = string.Empty;
    public int Revision { get; set; }
}
