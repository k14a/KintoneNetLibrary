using KintoneNetLibrary.CodeGen.Domain.Models;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface INameTableApplicable {
    /// <summary>
    /// 変換テーブルを読み込み、プロパティ名変換に適用する。
    /// </summary>
    void LoadNameTable(NameTable table);
}
