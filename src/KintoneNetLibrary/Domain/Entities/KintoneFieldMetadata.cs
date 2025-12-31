namespace KintoneNetLibrary.Domain.Entities;

public class KintoneFieldMetadata {
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
    public KintoneFieldType Type { get; set; }
    public bool Required { get; set; }

    // 選択肢（ラジオ、ドロップダウン、チェックボックス）
    public IReadOnlyList<string>? Options { get; set; }

    // サブテーブルの場合のみ
    public IReadOnlyList<KintoneFieldMetadata>? SubFields { get; set; }
}
