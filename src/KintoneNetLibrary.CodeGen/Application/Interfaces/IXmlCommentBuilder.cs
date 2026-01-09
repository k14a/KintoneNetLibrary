using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

public interface IXmlCommentBuilder {
    /// <summary>
    /// フィールド用 XML コメントを生成する
    /// </summary>
    string BuildForField(KintoneFieldSchema field);

    /// <summary>
    /// サブテーブル用 XML コメントを生成する
    /// </summary>
    string BuildForSubTable(KintoneSubTableSchema subTable);
}