namespace KintoneNetLibrary.CodeGen.Domain.Models;

public class NameTable : Dictionary<string, FieldNameMapping> {
    public NameTable() : base(StringComparer.Ordinal) { }

    /// <summary>
    /// 指定したフィールドコードのマッピングを取得する。
    /// 存在しない場合は null を返す。
    /// </summary>
    public FieldNameMapping? TryGet(string fieldCode) {
        return this.TryGetValue(fieldCode, out var mapping)
            ? mapping
            : null;
    }

    /// <summary>
    /// マッピングを追加または更新する。
    /// </summary>
    public void Set(string fieldCode, FieldNameMapping mapping) {
        this[fieldCode] = mapping;
    }

    /// <summary>
    /// 別の NameTable をマージする。
    /// 既存の property が空欄の場合のみ上書きする。
    /// </summary>
    public void Merge(NameTable other) {
        foreach (var (code, mapping) in other) {
            if (!this.ContainsKey(code)) {
                this[code] = mapping;
                continue;
            }

            // 既存の property が空欄なら上書き
            if (string.IsNullOrWhiteSpace(this[code].Property)) {
                this[code].Property = mapping.Property;
            }
        }
    }
}
