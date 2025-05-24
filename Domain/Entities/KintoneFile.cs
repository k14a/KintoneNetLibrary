namespace KintoneNetLibrary.Domain.Entities;

public class KintoneFile
{
    public string ContentType { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
}
