namespace KintoneNetLibrary.Domain.Entities;

public class KintoneAppMetadata {
    public int AppId { get; set; }
    public IReadOnlyList<KintoneFieldMetadata> Fields { get; set; } = default!;
}
