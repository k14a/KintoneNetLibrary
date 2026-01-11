using KintoneNetLibrary.CodeGen.Domain.Schemas;

namespace KintoneNetLibrary.CodeGen.Application.Interfaces;

/// <summary>
/// XML コメントビルダーインターフェース
/// </summary>
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